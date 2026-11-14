#!/usr/bin/env bash
# One-time, idempotent preparation of the server for the Azure DevOps deploys. Re-running it is safe: it
# fixes what is missing and skips what is done. It never restarts Docker or a running container.
#
# Usage:  sudo AGENT_USER=<user the Azure DevOps agent runs as> deploy/host/bootstrap-host.sh
#   find it:  ps -o user= -p "$(pgrep -f Agent.Listener | head -n1)"
#
# Optional env:
#   ENVS="staging production"   one folder per environment under /opt/ecommerce
#   SWAP_GB=4                   swap file size when the server has no swap at all (0 = skip)
#   BUILD_CACHE_KEEP=10gb       build cache the weekly cleanup keeps (newest first)
set -euo pipefail

die() { echo "ERROR: $*" >&2; exit 1; }
[[ $EUID -eq 0 ]] || die "run with sudo"
AGENT_USER="${AGENT_USER:?set AGENT_USER (the Linux user the Azure DevOps agent runs as)}"
id "$AGENT_USER" >/dev/null 2>&1 || die "user $AGENT_USER does not exist"
ENVS="${ENVS:-staging production}"
SWAP_GB="${SWAP_GB:-4}"
BUILD_CACHE_KEEP="${BUILD_CACHE_KEEP:-10gb}"
[[ "$BUILD_CACHE_KEEP" =~ ^[0-9]+(mb|gb)$ ]] || die "BUILD_CACHE_KEEP must look like 10gb or 500mb"
# Fixed on purpose: Traefik gets 172.29.0.2 on this network, and the API trusts exactly that address
# (deploy/edge/compose.yml, deploy/app.compose.yml). Change all three together.
EDGE_SUBNET=172.29.0.0/24
EDGE_GW=172.29.0.1
DEPLOY_ROOT=/opt/ecommerce
AGENT_GROUP="$(id -gn "$AGENT_USER")"

command -v docker >/dev/null || die "Docker Engine is not installed (docs.docker.com/engine/install/ubuntu)"
if ip route | grep -v 'br-' | grep -q '^172\.29\.0\.'; then
  die "the edge subnet $EDGE_SUBNET overlaps an existing route: $(ip route | grep '^172\.29\.0\.')"
fi
if [[ -d "$DEPLOY_ROOT/.git" ]]; then
  die "$DEPLOY_ROOT is a git clone (the old timer-based deploy). Remove it first: deploy/README.md, 'Moving from the timer deploy'"
fi

echo "==> Packages"
pkgs=(jq curl)
missing=()
for p in "${pkgs[@]}"; do dpkg -s "$p" >/dev/null 2>&1 || missing+=("$p"); done
if ((${#missing[@]})); then
  # needrestart in list-only mode: installing a package must not restart any running service.
  export NEEDRESTART_MODE=l DEBIAN_FRONTEND=noninteractive
  apt-get update
  apt-get install -y "${missing[@]}"
else
  echo "already installed: ${pkgs[*]}"
fi

echo "==> $AGENT_USER in the docker group (root-equivalent: use this user for nothing else)"
if id -nG "$AGENT_USER" | grep -qw docker; then
  echo "already a member"
else
  usermod -aG docker "$AGENT_USER"
  echo "added: restart the agent service so it gets the group (cd <agent folder> && sudo ./svc.sh stop && sudo ./svc.sh start)"
fi

echo "==> Docker live-restore (containers keep running while the Docker daemon restarts, e.g. on an upgrade)"
f=/etc/docker/daemon.json
[[ -f "$f" ]] || echo '{}' >"$f"
if [[ "$(jq -r '."live-restore" // false' "$f")" != true ]]; then
  cp "$f" "$f.bak.$(date +%Y%m%d%H%M%S)"
  jq '. + {"live-restore": true}' "$f" >"$f.new"
  mv "$f.new" "$f"
  systemctl reload docker # SIGHUP: applies live-restore without restarting anything
fi
# The reload is asynchronous: give dockerd a moment to read daemon.json again.
for _ in $(seq 1 15); do
  [[ "$(docker info --format '{{.LiveRestoreEnabled}}')" == true ]] && break
  sleep 1
done
[[ "$(docker info --format '{{.LiveRestoreEnabled}}')" == true ]] || die "live-restore is not active after 15 s: check $f"
echo "live-restore: on"

echo "==> Docker network 'edge' $EDGE_SUBNET (Traefik at 172.29.0.2)"
docker network inspect edge >/dev/null 2>&1 || docker network create --subnet "$EDGE_SUBNET" --gateway "$EDGE_GW" edge

echo "==> Release folders: $DEPLOY_ROOT/<env>, owned by $AGENT_USER, closed to other users"
install -d -o root -g "$AGENT_GROUP" -m 0750 "$DEPLOY_ROOT"
chown "root:$AGENT_GROUP" "$DEPLOY_ROOT"
chmod 0750 "$DEPLOY_ROOT"
for e in $ENVS; do
  [[ "$e" =~ ^[a-z][a-z0-9-]*$ ]] || die "environment names must be lowercase (got '$e')"
  install -d -o "$AGENT_USER" -g "$AGENT_GROUP" -m 0750 "$DEPLOY_ROOT/$e"
  echo "    $DEPLOY_ROOT/$e"
done

echo "==> Edge folders (/opt/edge, certificates in /opt/edge/certs, 0700)"
install -d -m 0755 /opt/edge /opt/edge/dynamic
install -d -m 0700 /opt/edge/certs

echo "==> Weekly build-cache cleanup (keeps the newest $BUILD_CACHE_KEEP)"
# Size-based, not age-based: base images and the restore layers are reused by every build but can sit
# unused for weeks; an age filter would delete them and the next build would download everything again.
cat >/etc/cron.weekly/docker-builder-prune <<EOF
#!/bin/sh
# Release images are removed by deploy.sh. Never run 'docker system prune -a' on this server:
# it deletes the images a rollback needs.
docker builder prune -f --reserved-space $BUILD_CACHE_KEEP >/dev/null 2>&1 || true
EOF
chmod 0755 /etc/cron.weekly/docker-builder-prune

if [[ "$SWAP_GB" != 0 ]] && [[ -z "$(swapon --show --noheadings)" ]]; then
  echo "==> Swap file ${SWAP_GB} GB (a safety net for image builds on a small server)"
  fallocate -l "${SWAP_GB}G" /swapfile
  chmod 600 /swapfile
  mkswap /swapfile
  swapon /swapfile
  grep -q '^/swapfile ' /etc/fstab || echo '/swapfile none swap sw 0 0' >>/etc/fstab
else
  echo "==> Swap: $(swapon --show --noheadings | awk '{print $1, $3}' | tr '\n' ' ')(unchanged)"
fi

if systemctl is-active --quiet ecommerce-deploy.timer 2>/dev/null; then
  echo
  echo "WARNING: ecommerce-deploy.timer (the old pull deploy) is active. Only one deployer per server:"
  echo "  sudo systemctl disable --now ecommerce-deploy.timer"
fi

echo
echo "Done. Nothing was restarted."
echo "Next:"
echo "  sudo deploy/host/install-edge.sh"
echo "  sudo AGENT_USER=$AGENT_USER deploy/host/install-infra.sh"
