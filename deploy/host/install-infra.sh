#!/usr/bin/env bash
# Install or update the shared infrastructure (PostgreSQL, Redis, the observability stack) in
# /opt/ecommerce/infra, from infra.compose.yml and its config folders in this clone. Both environments use
# it: one PostgreSQL with a database per environment, one Redis with a database number per environment.
# Re-running it applies config changes; data stays in the named volumes.
#
# Usage:  sudo AGENT_USER=<user the Azure DevOps agent runs as> deploy/host/install-infra.sh
#
# Needs /etc/ecommerce/infra.env, mode 0600, owned by AGENT_USER, with POSTGRES_DB, POSTGRES_USER and
# POSTGRES_PASSWORD. The variable groups use the same POSTGRES_USER and POSTGRES_PASSWORD.
set -euo pipefail

die() { echo "ERROR: $*" >&2; exit 1; }
[[ $EUID -eq 0 ]] || die "run with sudo"
AGENT_USER="${AGENT_USER:?set AGENT_USER (the Linux user the Azure DevOps agent runs as)}"
id "$AGENT_USER" >/dev/null 2>&1 || die "user $AGENT_USER does not exist"
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
DEST=/opt/ecommerce/infra
ENV_FILE=/etc/ecommerce/infra.env
AGENT_GROUP="$(id -gn "$AGENT_USER")"

[[ -d /opt/ecommerce ]] || die "/opt/ecommerce is missing: run bootstrap-host.sh first"
[[ -f "$ENV_FILE" ]] || die "$ENV_FILE is missing (deploy/README.md, step 3)"
[[ "$(stat -c '%a %U' "$ENV_FILE")" == "600 $AGENT_USER" ]] \
  || die "$ENV_FILE must be mode 600 and owned by $AGENT_USER (it is $(stat -c '%a %U' "$ENV_FILE"))"
for k in POSTGRES_DB POSTGRES_USER POSTGRES_PASSWORD; do
  grep -Eq "^$k=.+" "$ENV_FILE" || die "$ENV_FILE has no value for $k"
done

echo "==> Copying infra.compose.yml and its config to $DEST"
install -d -o "$AGENT_USER" -g "$AGENT_GROUP" -m 0750 "$DEST"
install -m 0644 "$REPO/infra.compose.yml" "$DEST/infra.compose.yml"
# The containers read these files under their own users, so they are world-readable (no secrets in them).
for d in otel loki tempo prometheus grafana/provisioning; do
  (cd "$REPO" && find "$d" -type d -name data -prune -o -type d -print) | while read -r dir; do
    install -d -m 0755 "$DEST/$dir"
  done
  (cd "$REPO" && find "$d" -type d -name data -prune -o -type f -print) | while read -r file; do
    install -m 0644 "$REPO/$file" "$DEST/$file"
  done
done

echo "==> docker compose up (project ecommerce-infra) as $AGENT_USER"
sudo -u "$AGENT_USER" docker compose -f "$DEST/infra.compose.yml" --env-file "$ENV_FILE" up -d --remove-orphans

echo "==> Waiting for PostgreSQL and Redis to be healthy"
for c in database-postgres cache-redis; do
  for _ in $(seq 1 30); do
    [[ "$(docker inspect -f '{{if .State.Health}}{{.State.Health.Status}}{{end}}' "$c" 2>/dev/null)" == healthy ]] && break
    sleep 2
  done
  [[ "$(docker inspect -f '{{if .State.Health}}{{.State.Health.Status}}{{end}}' "$c" 2>/dev/null)" == healthy ]] \
    || die "$c is not healthy after 60 s: docker logs $c"
  echo "$c: healthy"
done
docker ps --filter label=com.docker.compose.project=ecommerce-infra --format 'table {{.Names}}\t{{.Status}}'
