#!/usr/bin/env bash
# Deploys one release of the API to one environment on this server. The Azure DevOps deploy stage
# (.azure-pipelines/templates/deploy-stage.yml) runs it as the agent user, on the server itself.
#
#   bluegreen  (default) The new container starts in the other slot (blue/green) next to the running one.
#              Traefik sends it traffic only once its healthcheck passes; then the old one stops and the
#              smoke test runs. No downtime. A release that never gets healthy never touches the running one.
#   recreate   Stop the running container, start the new one in the same slot. A few seconds of downtime.
#
# Steps: preflight (nothing changes if any of it fails), release folder and .env, migrations, switch,
# smoke test through Traefik, rollback on failure, retention.
#
# Required env: ENV_NAME IMAGE_TAG BUNDLE_DIR PUBLIC_HOST REDIS_DATABASE
#               POSTGRES_DB POSTGRES_USER POSTGRES_PASSWORD JWT_SECRET_KEY
# Optional env: STRATEGY          bluegreen | recreate (default bluegreen)
#               JWT_ISSUER JWT_AUDIENCE API_MEM_LIMIT
#               SMOKE_INSECURE    true = no certificate check in the smoke test (Traefik's self-signed default
#                                 certificate, before the server has a real one)
#               API_IMAGE MIGRATIONS_IMAGE   local image names (default ecommerce-api, ecommerce-migrations)
#               DEPLOY_ROOT       default /opt/ecommerce
#               KEEP_RELEASES     release folders kept per environment (default 5)
#               HEALTH_TIMEOUT    seconds to wait for a healthy container (default 120)
#
# Layout:  $DEPLOY_ROOT/<env>/releases/<timestamp>_<tag>/   app.compose.yml and .env of one deploy
#          $DEPLOY_ROOT/<env>/current -> releases/...        the release that is running
set -euo pipefail
umask 077 # the release .env holds the environment's secrets: owner only

die() { echo "ERROR: $*" >&2; exit 1; }
log() { printf '\n==> %s\n' "$*"; }
# Azure DevOps passes "$(NAME)" literally when the variable is not defined: unset those. Checking the value
# alone is not enough: docker compose prefers the environment over the release .env, so an optional variable
# left out of the group (API_MEM_LIMIT, JWT_ISSUER) would reach the container as the text "$(NAME)".
for k in $(compgen -e); do
  # shellcheck disable=SC2016 # the single-quoted '$(' is that literal text, not an expansion
  if [[ "${!k}" == '$('*')' ]]; then unset "$k"; fi
done
val() { local v="${!1-}"; printf '%s' "${v:-${2-}}"; } # value of variable $1, or default $2

REQUIRED=(ENV_NAME IMAGE_TAG BUNDLE_DIR PUBLIC_HOST REDIS_DATABASE POSTGRES_DB POSTGRES_USER POSTGRES_PASSWORD JWT_SECRET_KEY)
for k in "${REQUIRED[@]}"; do
  [[ -n "$(val "$k")" ]] || die "$k is not set (add it to the environment's variable group)"
done

ENV_NAME="$(val ENV_NAME)"
IMAGE_TAG="$(val IMAGE_TAG)"
BUNDLE_DIR="$(val BUNDLE_DIR)"
PUBLIC_HOST="$(val PUBLIC_HOST)"
STRATEGY="$(val STRATEGY bluegreen)"
SMOKE_INSECURE="$(val SMOKE_INSECURE false)"
API_IMAGE="$(val API_IMAGE ecommerce-api)"
MIGRATIONS_IMAGE="$(val MIGRATIONS_IMAGE ecommerce-migrations)"
DEPLOY_ROOT="$(val DEPLOY_ROOT /opt/ecommerce)"
KEEP_RELEASES="$(val KEEP_RELEASES 5)"
HEALTH_TIMEOUT="$(val HEALTH_TIMEOUT 120)"
KEEP_IMAGES=6 # newest image tags kept per image, on top of every tag a kept release still uses

[[ "$ENV_NAME" =~ ^[a-z][a-z0-9-]*$ ]] || die "ENV_NAME must be lowercase, e.g. staging (got '$ENV_NAME')"
[[ "$IMAGE_TAG" =~ ^[A-Za-z0-9_.-]+$ ]] || die "IMAGE_TAG has characters a Docker tag can not have: '$IMAGE_TAG'"
[[ "$STRATEGY" == bluegreen || "$STRATEGY" == recreate ]] || die "STRATEGY must be bluegreen or recreate (got '$STRATEGY')"
[[ "$(val REDIS_DATABASE)" =~ ^[0-9]+$ ]] || die "REDIS_DATABASE must be a number (0-15), one per environment"
[[ "$KEEP_RELEASES" =~ ^[0-9]+$ ]] && ((KEEP_RELEASES >= 2)) || KEEP_RELEASES=2
# The API refuses to start with a shorter key; failing here keeps the running release untouched.
(($(printf '%s' "$(val JWT_SECRET_KEY)" | wc -c) >= 32)) || die "JWT_SECRET_KEY must be at least 32 bytes (openssl rand -base64 48)"

ROUTER="ecommerce-api-$ENV_NAME" # Traefik router and service name, deploy.app label
ENV_DIR="$DEPLOY_ROOT/$ENV_NAME"
RELEASES="$ENV_DIR/releases"
CURRENT="$ENV_DIR/current"
NEW="$RELEASES/$(date -u +%Y%m%d_%H%M%S)_$IMAGE_TAG"

envval() { sed -n "s/^$2='\(.*\)'$/\1/p" "$1/.env" 2>/dev/null | head -n1; } # a value from a release's .env
slot_of() { local s; s="$(envval "$1" SLOT)"; printf '%s' "${s:-blue}"; }
project_of() { printf 'ecommerce-%s-%s' "$ENV_NAME" "$(slot_of "$1")"; }

compose() {
  local dir="$1"
  shift
  docker compose -p "$(project_of "$dir")" --project-directory "$dir" -f "$dir/app.compose.yml" --env-file "$dir/.env" "$@"
}

container_id() { compose "$1" ps -aq api 2>/dev/null | head -n1; }

wait_healthy() {
  local cid status="starting" deadline=$((SECONDS + HEALTH_TIMEOUT))
  cid="$(container_id "$1")"
  [[ -n "$cid" ]] || { echo "No container found"; return 1; }
  echo "Waiting up to ${HEALTH_TIMEOUT}s for the container to be healthy..."
  while ((SECONDS < deadline)); do
    # (docker inspect prints an empty line for a missing container, so "|| echo gone" inside $() won't do)
    status="$(docker inspect -f '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' "$cid" 2>/dev/null)" || status=gone
    case "$status" in
      healthy) echo "Container: healthy"; return 0 ;;
      unhealthy | exited | dead | gone) echo "Container: $status"; return 1 ;;
    esac
    sleep 3
  done
  echo "Timed out (last status: $status)"
  return 1
}

# Through the local Traefik with the real hostname, so routing and TLS are checked too, before any DNS
# or hosts entry points at the server.
smoke() {
  local url="https://$PUBLIC_HOST/health" body i
  local -a opts=(-sS --fail-with-body --max-time 20 --resolve "$PUBLIC_HOST:443:127.0.0.1")
  [[ "$SMOKE_INSECURE" == true ]] && opts+=(-k)
  echo "Smoke test: $url (certificate check $([[ "$SMOKE_INSECURE" == true ]] && echo OFF || echo ON))"
  for i in 1 2 3 4 5 6; do
    if body="$(curl "${opts[@]}" "$url" 2>&1)"; then
      if [[ "$body" == Healthy ]]; then
        echo "Smoke OK"
        return 0
      fi
      echo "Attempt $i/6: unexpected body: ${body:0:200}"
    else
      echo "Attempt $i/6 failed: ${body:0:300}"
    fi
    sleep 5
  done
  return 1
}

diagnostics() {
  echo "--- DIAGNOSTICS ---"
  compose "$1" ps -a || true
  local cid
  cid="$(container_id "$1")"
  if [[ -n "$cid" ]]; then
    docker inspect -f '{{json .State.Health}}' "$cid" 2>/dev/null || true
    docker logs --tail 100 "$cid" 2>&1 || true
  fi
  # Traefik's access log names the router ("ecommerce-api-staging@docker"), its errors may name the host.
  docker logs --since 10m traefik 2>&1 | grep -F -e "$PUBLIC_HOST" -e "$ROUTER@" | tail -n 20 || true
}

retention() {
  log "Retention: last $KEEP_RELEASES releases; newest $KEEP_IMAGES images plus every tag a kept release uses"
  local cur keep d t image
  cur="$(readlink -f "$CURRENT")"
  find "$RELEASES" -mindepth 1 -maxdepth 1 -type d | sort -r | tail -n +$((KEEP_RELEASES + 1)) | while read -r d; do
    [[ "$(readlink -f "$d")" == "$cur" ]] && continue
    echo "Removing release $d"
    rm -rf "$d"
  done
  for image in "$API_IMAGE" "$MIGRATIONS_IMAGE"; do
    keep="$({
      for d in "$DEPLOY_ROOT"/*/releases/*/; do envval "$d" IMAGE_TAG; done
      docker image ls "$image" --format '{{.Tag}}' | head -n "$KEEP_IMAGES"
    } | sort -u || true)"
    docker image ls "$image" --format '{{.Tag}}' | while read -r t; do
      if ! grep -qxF -- "$t" <<<"$keep"; then
        echo "Removing image $image:$t"
        docker image rm "$image:$t" >/dev/null 2>&1 || true
      fi
    done
  done
}

# ------------------------------------------------------------------ main ----
[[ -d "$ENV_DIR" ]] || die "$ENV_DIR is missing: run deploy/host/bootstrap-host.sh on this server first"
mkdir -p "$RELEASES"
PREV="$(readlink -f "$CURRENT" 2>/dev/null || true)"
[[ -d "$PREV" ]] || PREV=""
PREV_RUNNING=""
if [[ -n "$PREV" && -n "$(compose "$PREV" ps -q api 2>/dev/null)" ]]; then PREV_RUNNING=1; fi

if [[ -z "$PREV_RUNNING" ]]; then
  SLOT="$([[ -n "$PREV" ]] && slot_of "$PREV" || echo blue)"
elif [[ "$STRATEGY" == bluegreen ]]; then
  SLOT="$([[ "$(slot_of "$PREV")" == blue ]] && echo green || echo blue)"
else
  SLOT="$(slot_of "$PREV")"
fi

cat <<EOF
==========================================
 Environment : $ENV_NAME
 Strategy    : $STRATEGY   (new slot: $SLOT, container $ROUTER-$SLOT)
 Images      : $API_IMAGE:$IMAGE_TAG, $MIGRATIONS_IMAGE:$IMAGE_TAG
 Smoke URL   : https://$PUBLIC_HOST/health
 Release     : $NEW
 Previous    : ${PREV:-(none, first deploy)}${PREV:+ (slot $(slot_of "$PREV"), $([[ -n "$PREV_RUNNING" ]] && echo running || echo stopped))}
==========================================
EOF

log "Preflight (nothing is changed if any of this fails)"
for image in "$API_IMAGE" "$MIGRATIONS_IMAGE"; do
  docker image inspect "$image:$IMAGE_TAG" >/dev/null 2>&1 \
    || die "image $image:$IMAGE_TAG is not on this server. Run the whole pipeline again to rebuild it."
done
for network in edge ecommerce-network; do
  docker network inspect "$network" >/dev/null 2>&1 \
    || die "docker network '$network' is missing: run deploy/host/bootstrap-host.sh and install-infra.sh"
done
[[ "$(docker inspect -f '{{.State.Running}}' traefik 2>/dev/null)" == true ]] \
  || die "traefik is not running: run deploy/host/install-edge.sh"
for c in database-postgres cache-redis; do
  [[ "$(docker inspect -f '{{if .State.Health}}{{.State.Health.Status}}{{end}}' "$c" 2>/dev/null)" == healthy ]] \
    || die "$c is not healthy: check the infrastructure stack (deploy/host/install-infra.sh)"
done
# Values go into the .env single-quoted (taken literally by docker compose), so a ' or a line break can't be in one.
for k in "${REQUIRED[@]}" JWT_ISSUER JWT_AUDIENCE API_MEM_LIMIT; do
  v="$(val "$k")"
  [[ "$v" != *"'"* && "$v" != *$'\n'* ]] || die "$k contains a ' or a line break, which the .env file can not hold"
done

log "Preparing the release folder"
mkdir -p "$NEW"
cp "$BUNDLE_DIR/app.compose.yml" "$NEW/"
{
  printf "%s='%s'\n" \
    SLOT "$SLOT" \
    CONTAINER_NAME "$ROUTER-$SLOT" \
    ROUTER "$ROUTER" \
    API_IMAGE "$API_IMAGE" \
    MIGRATIONS_IMAGE "$MIGRATIONS_IMAGE" \
    IMAGE_TAG "$IMAGE_TAG" \
    PUBLIC_HOST "$PUBLIC_HOST" \
    ASPNETCORE_ENVIRONMENT "${ENV_NAME^}" \
    POSTGRES_DB "$(val POSTGRES_DB)" \
    POSTGRES_USER "$(val POSTGRES_USER)" \
    POSTGRES_PASSWORD "$(val POSTGRES_PASSWORD)" \
    JWT_SECRET_KEY "$(val JWT_SECRET_KEY)" \
    REDIS_DATABASE "$(val REDIS_DATABASE)"
  for k in JWT_ISSUER JWT_AUDIENCE API_MEM_LIMIT; do
    if [[ -n "$(val "$k")" ]]; then printf "%s='%s'\n" "$k" "$(val "$k")"; fi
  done
} >"$NEW/.env"
compose "$NEW" config -q || { rm -rf "$NEW"; die "app.compose.yml is not valid with this .env"; }

log "Migrations (the running release keeps serving: each migration must work with the previous release too)"
if ! compose "$NEW" --profile tools run --rm migrate; then
  die "migrations failed: nothing was switched. Release kept for inspection: $NEW"
fi

# --------------------------------------------------------------- bluegreen --
if [[ "$STRATEGY" == bluegreen && -n "$PREV_RUNNING" ]]; then
  log "Starting $ROUTER-$SLOT next to the running $ROUTER-$(slot_of "$PREV") (no traffic until healthy)"
  if ! { compose "$NEW" up -d --remove-orphans && wait_healthy "$NEW"; }; then
    log "DEPLOYMENT FAILED: the new container never became healthy. The running release was not touched."
    diagnostics "$NEW"
    compose "$NEW" down --remove-orphans || true
    echo "Failed release kept for inspection: $NEW"
    exit 1
  fi
  sleep 5 # let Traefik add the new container to the service
  log "Stopping the old container ($ROUTER-$(slot_of "$PREV")); the new one takes all traffic"
  compose "$PREV" stop || log "WARNING: could not stop the old container; both serve until it is removed below"
  if smoke; then
    compose "$PREV" down --remove-orphans || true
    ln -sfn "$NEW" "$CURRENT"
    log "Deployment OK"
  else
    log "SMOKE TEST FAILED: starting the previous container again, removing the new one"
    compose "$PREV" start || true
    wait_healthy "$PREV" || log "THE PREVIOUS CONTAINER DID NOT COME BACK: check now (docker ps -a)"
    diagnostics "$NEW"
    compose "$NEW" down --remove-orphans || true
    echo "Failed release kept for inspection: $NEW"
    exit 1
  fi

# ---------------------------------------------------------------- recreate --
else
  if [[ -n "$PREV_RUNNING" ]]; then
    log "Stopping $ROUTER-$SLOT (strategy recreate: short downtime while the new one starts)"
    compose "$PREV" down --remove-orphans
  fi
  log "Starting $ROUTER-$SLOT from $NEW"
  if compose "$NEW" up -d --remove-orphans && wait_healthy "$NEW" && smoke; then
    ln -sfn "$NEW" "$CURRENT"
    log "Deployment OK"
  else
    log "DEPLOYMENT FAILED"
    diagnostics "$NEW"
    compose "$NEW" down --remove-orphans || true
    if [[ -n "$PREV" ]]; then
      log "ROLLBACK -> $PREV"
      if compose "$PREV" up -d --remove-orphans && wait_healthy "$PREV"; then
        log "Rollback done: the previous release is serving"
      else
        log "ROLLBACK FAILED. By hand: docker compose -p $(project_of "$PREV") --project-directory $PREV -f $PREV/app.compose.yml --env-file $PREV/.env up -d"
      fi
    else
      log "No previous release (first deploy): nothing to roll back to"
    fi
    echo "Failed release kept for inspection: $NEW"
    exit 1
  fi
fi

retention
log "Deployment complete"
