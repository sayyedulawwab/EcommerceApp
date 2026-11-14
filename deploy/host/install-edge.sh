#!/usr/bin/env bash
# Install or update Traefik (deploy/edge) in /opt/edge, then check it answers on 443.
#
# Usage:  sudo deploy/host/install-edge.sh
set -euo pipefail

die() { echo "ERROR: $*" >&2; exit 1; }
[[ $EUID -eq 0 ]] || die "run with sudo"
SRC="$(cd "$(dirname "${BASH_SOURCE[0]}")/../edge" && pwd)"

docker network inspect edge >/dev/null 2>&1 || die "network 'edge' is missing: run bootstrap-host.sh first"

install -d -m 0755 /opt/edge /opt/edge/dynamic
install -d -m 0700 /opt/edge/certs
install -m 0644 "$SRC/compose.yml" /opt/edge/compose.yml
install -m 0644 "$SRC"/dynamic/*.yml /opt/edge/dynamic/

# Every certificate file tls.yml names must exist (comment lines are skipped).
grep -v '^\s*#' /opt/edge/dynamic/tls.yml | grep -Eo '/certs/[^ ]+' | sort -u | while read -r p; do
  [[ -f "/opt/edge$p" ]] || die "tls.yml names /opt/edge$p, which does not exist"
done

if ! docker ps --format '{{.Names}}' | grep -qx traefik; then
  if ss -tlnp | grep -Eq ':(80|443)\s'; then
    ss -tlnp | grep -E ':(80|443)\s'
    die "ports 80/443 are already in use by the process(es) above"
  fi
fi

cd /opt/edge
docker compose pull --quiet
docker compose up -d
sleep 5
docker compose ps

ip="$(docker inspect -f '{{(index .NetworkSettings.Networks "edge").IPAddress}}' traefik)"
[[ "$ip" == 172.29.0.2 ]] || die "traefik has $ip on 'edge', expected 172.29.0.2 (the address the API trusts)"

echo
echo "==> Probe https://127.0.0.1 through Traefik (HTTP 404 is expected until an environment is deployed)"
code="$(curl -sk -o /dev/null -w '%{http_code}' https://127.0.0.1/ || true)"
echo "HTTP $code"
[[ "$code" =~ ^(200|404)$ ]] || die "unexpected HTTP $code (000 = Traefik is not answering: docker logs traefik)"
echo "Edge OK"
