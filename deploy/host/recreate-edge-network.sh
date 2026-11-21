#!/usr/bin/env bash
# One-off fix: recreate the 'edge' network with an ip-range, so Docker never gives Traefik's fixed
# address (172.29.0.2) to another container after a reboot. Then start Traefik and reconnect the rest.
set -euo pipefail
SUBNET=172.29.0.0/24
GW=172.29.0.1
RANGE=172.29.0.128/25   # automatic addresses come only from .128-.255; .2 stays free for Traefik

echo "==> Before"
docker network inspect edge --format '{{range .Containers}}{{.Name}} {{.IPv4Address}}{{println}}{{end}}'

mapfile -t members < <(docker network inspect edge --format '{{range .Containers}}{{.Name}}{{println}}{{end}}' | sed '/^$/d')
for c in "${members[@]}"; do docker network disconnect -f edge "$c"; done

# The exited Traefik container still points at the old network; compose creates a fresh one.
docker rm -f traefik >/dev/null 2>&1 || true

docker network rm edge
docker network create --subnet "$SUBNET" --gateway "$GW" --ip-range "$RANGE" edge

echo "==> Starting Traefik"
cd /opt/edge && docker compose up -d

for c in "${members[@]}"; do
  [[ "$c" == traefik ]] || docker network connect edge "$c"
done

echo "==> After"
docker network inspect edge --format '{{range .Containers}}{{.Name}} {{.IPv4Address}}{{println}}{{end}}'
sleep 3
curl -sk -o /dev/null -w 'Traefik on 443: HTTP %{http_code} (200 or 404 = answering)\n' https://127.0.0.1/
