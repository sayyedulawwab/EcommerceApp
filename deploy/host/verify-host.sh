#!/usr/bin/env bash
# Checks the server before the first deploy (and any time after). Changes nothing.
#
# Usage:  sudo AGENT_USER=<user the Azure DevOps agent runs as> deploy/host/verify-host.sh
#   optional: ENVS="staging production"
# Each check is "test && ok ... || bad ...": ok, bad and note always succeed, so exactly one of them runs.
# shellcheck disable=SC2015
set -uo pipefail

AGENT_USER="${AGENT_USER:?set AGENT_USER (the Linux user the Azure DevOps agent runs as)}"
ENVS="${ENVS:-staging production}"

pass=0 fail=0 warn=0
ok() { printf '  \e[32mPASS\e[0m  %s\n' "$1"; pass=$((pass + 1)); }
bad() { printf '  \e[31mFAIL\e[0m  %s\n' "$1"; [[ -n "${2:-}" ]] && printf '        -> %s\n' "$2"; fail=$((fail + 1)); }
note() { printf '  \e[33mWARN\e[0m  %s\n' "$1"; [[ -n "${2:-}" ]] && printf '        -> %s\n' "$2"; warn=$((warn + 1)); }
health() {
  local s
  s="$(docker inspect -f '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' "$1" 2>/dev/null)" || s=missing
  printf '%s' "$s"
}

echo "== Agent"
if id "$AGENT_USER" >/dev/null 2>&1; then
  id -nG "$AGENT_USER" | grep -qw docker && ok "$AGENT_USER is in the docker group" || bad "$AGENT_USER is not in the docker group" "bootstrap-host.sh, then restart the agent service"
  sudo -u "$AGENT_USER" docker info >/dev/null 2>&1 && ok "docker reachable as $AGENT_USER" || bad "docker not reachable as $AGENT_USER"
else
  bad "user $AGENT_USER does not exist"
fi
pgrep -u "$AGENT_USER" -f Agent.Listener >/dev/null && ok "agent running as $AGENT_USER" || bad "no Azure DevOps agent running as $AGENT_USER" "sudo ./svc.sh start in the agent folder"

echo "== Docker"
[[ "$(docker info --format '{{.LiveRestoreEnabled}}' 2>/dev/null)" == true ]] && ok "live-restore on" || bad "live-restore off" "bootstrap-host.sh"
subnet="$(docker network inspect -f '{{(index .IPAM.Config 0).Subnet}}' edge 2>/dev/null)"
[[ "$subnet" == 172.29.0.0/24 ]] && ok "network edge (172.29.0.0/24)" || bad "network edge missing or wrong subnet ('$subnet')" "bootstrap-host.sh"
docker network inspect ecommerce-network >/dev/null 2>&1 && ok "network ecommerce-network" || bad "network ecommerce-network missing" "install-infra.sh"
[[ -f /etc/cron.weekly/docker-builder-prune ]] && ok "weekly build-cache cleanup" || note "no weekly build-cache cleanup" "bootstrap-host.sh"

echo "== Edge"
[[ "$(health traefik)" == running ]] && ok "traefik running" || bad "traefik: $(health traefik)" "install-edge.sh"
[[ "$(health edge-socket-proxy)" == running ]] && ok "edge-socket-proxy running" || bad "edge-socket-proxy: $(health edge-socket-proxy)" "install-edge.sh"
ip="$(docker inspect -f '{{(index .NetworkSettings.Networks "edge").IPAddress}}' traefik 2>/dev/null)"
[[ "$ip" == 172.29.0.2 ]] && ok "traefik at 172.29.0.2 (the address the API trusts)" || bad "traefik address is '$ip'" "install-edge.sh"

echo "== Infrastructure"
for c in database-postgres cache-redis grafana; do
  [[ "$(health "$c")" == healthy ]] && ok "$c healthy" || bad "$c: $(health "$c")" "install-infra.sh; docker logs $c"
done
for c in otel-collector loki tempo prometheus; do
  [[ "$(health "$c")" =~ ^(running|healthy)$ ]] && ok "$c running" || bad "$c: $(health "$c")" "docker logs $c"
done

echo "== Folders"
for e in $ENVS; do
  [[ "$(stat -c %U "/opt/ecommerce/$e" 2>/dev/null)" == "$AGENT_USER" ]] && ok "/opt/ecommerce/$e owned by $AGENT_USER" || bad "/opt/ecommerce/$e missing or wrong owner" "bootstrap-host.sh"
done
[[ "$(stat -c '%a %U' /etc/ecommerce/infra.env 2>/dev/null)" == "600 $AGENT_USER" ]] && ok "/etc/ecommerce/infra.env: 600, $AGENT_USER" || bad "/etc/ecommerce/infra.env missing, or not 600 and owned by $AGENT_USER"

echo "== Host"
if systemctl is-active --quiet ecommerce-deploy.timer 2>/dev/null; then
  bad "ecommerce-deploy.timer (the old pull deploy) is active" "sudo systemctl disable --now ecommerce-deploy.timer"
else
  ok "no second deployer (ecommerce-deploy.timer inactive)"
fi
[[ "$(timedatectl show -p NTPSynchronized --value 2>/dev/null)" == yes ]] && ok "NTP synchronized" || bad "NTP not synchronized" "token expiry depends on the clock"
[[ -n "$(swapon --show --noheadings)" ]] && ok "swap: $(swapon --show --noheadings | awk '{print $3}' | paste -sd+)" || note "no swap" "bootstrap-host.sh SWAP_GB=4"
echo "  info  memory available: $(awk '/MemAvailable/ {printf "%.1f", $2/1048576}' /proc/meminfo) GB of $(awk '/MemTotal/ {printf "%.1f", $2/1048576}' /proc/meminfo) GB"
df -h /var/lib/docker 2>/dev/null | awk 'NR==2 {print "  info  /var/lib/docker: " $4 " free (" $5 " used)"}'

echo
echo "PASS $pass   FAIL $fail   WARN $warn"
((fail == 0))
