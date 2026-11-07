#!/usr/bin/env bash
# Deploys one release of the API on this server, and rolls back if it does not become healthy.
#
#   deploy.sh              follow the published "main" image (what the systemd timer runs)
#   deploy.sh --tag <sha>  deploy that exact commit; also how you roll back by hand
#
# A release is two images tagged with the commit SHA: ecommerce-migrations (the EF Core migration bundle)
# and ecommerce-api. Steps: pull both, run the bundle, switch the containers, wait for /health. If the
# bundle fails nothing is switched. If the new API is not healthy the previous release is started again.
#
# Settings (environment variables, all optional):
#   ENV_FILE          secrets file for docker compose, mode 0600 (default /etc/ecommerce/.env.prod)
#   STATE_DIR         where the current, previous and failed release tags are kept (default /var/lib/ecommerce-deploy)
#   APP_DIR           a clone of the repo holding the compose files (default: the folder above this script)
#   IMAGE_REPOSITORY  registry path of the images (default ghcr.io/sayyedulawwab)
#   SKIP_GIT_SYNC     1 = APP_DIR already holds the right commit, do not fetch (used by Azure Pipelines)
#   HEALTH_TIMEOUT    seconds to wait for the new API to become healthy (default 120)
#
# No secret is read, printed or stored here: docker compose reads ENV_FILE itself.

set -euo pipefail

main() {
  local script_dir
  script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

  ENV_FILE="${ENV_FILE:-/etc/ecommerce/.env.prod}"
  STATE_DIR="${STATE_DIR:-/var/lib/ecommerce-deploy}"
  APP_DIR="${APP_DIR:-$(dirname "$script_dir")}"
  IMAGE_REPOSITORY="${IMAGE_REPOSITORY:-ghcr.io/sayyedulawwab}"
  SKIP_GIT_SYNC="${SKIP_GIT_SYNC:-0}"
  HEALTH_TIMEOUT="${HEALTH_TIMEOUT:-120}"
  export IMAGE_REPOSITORY

  local requested=""
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --tag)
        [[ $# -ge 2 ]] || die "--tag needs a commit sha"
        requested="$2"
        shift 2
        ;;
      *)
        die "usage: deploy.sh [--tag <40-character commit sha>]"
        ;;
    esac
  done

  check_env_file
  mkdir -p "$STATE_DIR"

  # One deploy at a time: a timer run and a manual run must not overlap.
  exec 9> "$STATE_DIR/lock"
  if ! flock -n 9; then
    log "another deploy is running, nothing to do"
    return 0
  fi

  local tag
  if [[ -n "$requested" ]]; then
    tag="$requested"
    is_sha "$tag" || die "--tag must be a 40-character lowercase commit sha"
  else
    tag="$(find_published_tag)"
  fi

  local current failed
  current="$(read_state current)"
  failed="$(read_state failed)"

  if [[ "$tag" == "$current" ]]; then
    log "release $tag is already running, nothing to do"
    return 0
  fi
  if [[ -z "$requested" && "$tag" == "$failed" ]]; then
    log "release $tag failed before and was rolled back, waiting for a newer one (deploy.sh --tag $tag retries it)"
    return 0
  fi

  log "deploying release $tag (running now: ${current:-none})"

  sync_compose_files "$tag"
  pull_images "$tag"

  if ! compose_run "$tag" --profile tools run --rm migrate; then
    write_state failed "$tag"
    die "migrations failed, the running release was not touched"
  fi

  if switch_to "$tag"; then
    write_state previous "$current"
    write_state current "$tag"
    rm -f "$STATE_DIR/failed"
    log "release $tag is live"
    remove_old_images "$tag" "$current"
    return 0
  fi

  write_state failed "$tag"
  if [[ -n "$current" ]]; then
    log "release $tag is not healthy, rolling back to $current"
    if switch_to "$current"; then
      die "rolled back to $current"
    fi
    die "rollback to $current is not healthy either, check: docker logs ecommerce-api"
  fi
  die "release $tag is not healthy and there is no earlier release to roll back to"
}

log() {
  printf '%s deploy: %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$*"
}

die() {
  log "ERROR: $*" >&2
  exit 1
}

is_sha() {
  [[ "$1" =~ ^[0-9a-f]{40}$ ]]
}

# The env file holds the only secrets on this server, so it must be private to the user running the script.
check_env_file() {
  [[ -f "$ENV_FILE" ]] || die "$ENV_FILE not found"
  local mode owner
  mode="$(stat -c '%a' "$ENV_FILE")"
  owner="$(stat -c '%u' "$ENV_FILE")"
  [[ "$mode" == "600" ]] || die "$ENV_FILE must have mode 600 (it is $mode)"
  [[ "$owner" == "$(id -u)" ]] || die "$ENV_FILE must be owned by the user running the deploy"
}

read_state() {
  if [[ -f "$STATE_DIR/$1" ]]; then
    tr -d '[:space:]' < "$STATE_DIR/$1"
  fi
}

# Write to a temporary file first, so a crash never leaves a half-written state file.
write_state() {
  printf '%s\n' "$2" > "$STATE_DIR/$1.tmp"
  mv "$STATE_DIR/$1.tmp" "$STATE_DIR/$1"
}

# The moving "main" tag is published last, after the bundle and the API image of the same commit, and the
# image carries the commit it was built from as a label. Pulling "main" is cheap when nothing changed.
find_published_tag() {
  local image="$IMAGE_REPOSITORY/ecommerce-api:main" revision
  docker pull --quiet "$image" > /dev/null || die "could not pull $image"
  revision="$(docker image inspect --format '{{ index .Config.Labels "org.opencontainers.image.revision" }}' "$image")"
  is_sha "$revision" || die "$image has no valid org.opencontainers.image.revision label"
  printf '%s\n' "$revision"
}

# The compose files and the nginx config come from the same commit as the images.
sync_compose_files() {
  if [[ "$SKIP_GIT_SYNC" == "1" ]]; then
    return 0
  fi
  [[ -d "$APP_DIR/.git" ]] || die "$APP_DIR is not a git clone (set SKIP_GIT_SYNC=1 if it already holds the right files)"
  git -C "$APP_DIR" fetch --quiet origin main
  git -C "$APP_DIR" checkout --quiet --detach "$1" || die "commit $1 is not on origin/main"
}

pull_images() {
  local name
  for name in ecommerce-migrations ecommerce-api; do
    if ! docker image inspect "$IMAGE_REPOSITORY/$name:$1" > /dev/null 2>&1; then
      docker pull --quiet "$IMAGE_REPOSITORY/$name:$1" > /dev/null || die "could not pull $name:$1"
    fi
  done
}

compose_run() {
  local tag="$1"
  shift
  IMAGE_TAG="$tag" docker compose \
    --project-name ecommerce \
    --project-directory "$APP_DIR" \
    --env-file "$ENV_FILE" \
    --file "$APP_DIR/docker-compose.yml" \
    --file "$APP_DIR/docker-compose.prod.yml" \
    "$@"
}

# Start the containers for one release and wait until the API reports healthy. Returns 1 if it never does.
switch_to() {
  local tag="$1"
  compose_run "$tag" up --detach --no-build --remove-orphans || return 1
  wait_until_healthy || return 1
  # nginx resolves the API address when it loads its config, and the new container has a new address.
  compose_run "$tag" exec -T nginx nginx -s reload > /dev/null 2>&1 || log "warning: could not reload nginx"
}

wait_until_healthy() {
  local deadline status
  deadline=$((SECONDS + HEALTH_TIMEOUT))
  while ((SECONDS < deadline)); do
    status="$(docker inspect --format '{{ if .State.Health }}{{ .State.Health.Status }}{{ end }}' ecommerce-api 2> /dev/null || true)"
    case "$status" in
      healthy) return 0 ;;
      unhealthy) return 1 ;;
      *) sleep 5 ;;
    esac
  done
  return 1
}

# Keep the running release and the one before it (for a manual rollback); remove older tagged images.
remove_old_images() {
  local keep_a="$1" keep_b="$2" name ref
  for name in ecommerce-migrations ecommerce-api; do
    while IFS= read -r ref; do
      case "$ref" in
        *":$keep_a" | *":$keep_b" | *":main" | *":<none>") ;;
        *) docker image rm "$ref" > /dev/null 2>&1 || true ;;
      esac
    done < <(docker image ls --format '{{ .Repository }}:{{ .Tag }}' "$IMAGE_REPOSITORY/$name")
  done
}

main "$@"
exit
