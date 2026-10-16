#!/usr/bin/env bash
# Knight School - deploy / update the stack on this server
#
# Lives at:  /deploy/deploy.sh
#


set -euo pipefail # force stop execution if any command fails, invalid variable, or conditional failure on either side

DEPLOY_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(dirname "$DEPLOY_DIR")"
COMPOSE_FILE="$DEPLOY_DIR/compose.dev-server.yml"
LOCK_FILE="/tmp/knightschool-deploy.lock"
HEALTH_TIMEOUT_SECONDS=180

log() {
  echo "[deploy] $(date '+%Y-%m-%d %H:%M:%S') $*"
}

fail() {
  log "FAILED: $*"
  exit 1
}

compose() {
  docker compose -f "$COMPOSE_FILE" --project-directory "$DEPLOY_DIR" "$@"
}

# Retry a command a few times (the network may not be ready right after boot)
retry() {
  local attempt=1
  local max_attempts=5
  while true; do
    if "$@"; then
      return 0
    fi
    if [ "$attempt" -ge "$max_attempts" ]; then
      return 1
    fi
    log "Attempt $attempt failed; retrying in 10 seconds..."
    sleep 10
    attempt=$((attempt + 1))
  done
}

# Print the HTTP status code for a URL on this server ("000" = no answer).
# --resolve sends the request to this machine directly, but still checks the
# real certificate for the domain.
http_status() {
  curl -s -o /dev/null -w '%{http_code}' --max-time 5 \
    --resolve "$DOMAIN:443:127.0.0.1" "https://$DOMAIN$1" || true
}

health_check() {
  local waited=0
  local web_status
  local api_status

  while [ "$waited" -lt "$HEALTH_TIMEOUT_SECONDS" ]; do
    web_status="$(http_status "/")"
    # Any answer from the API (even 404) means it is up.
    # 502/503/504/000 mean Caddy could not reach it.
    api_status="$(http_status "/api/")"

    if [ "$web_status" = "200" ]; then
      case "$api_status" in
        000|502|503|504)
          ;;
        *)
          log "Healthy: web=$web_status api=$api_status"
          return 0
          ;;
      esac
    fi

    log "Waiting for the stack (web=$web_status api=$api_status)..."
    sleep 5
    waited=$((waited + 5))
  done

  log "Stack did not become healthy within ${HEALTH_TIMEOUT_SECONDS}s."
  compose ps
  log "Last 40 lines of the API log:"
  compose logs --tail 40 api
  return 1
}

main() {
  # Only one deploy at a time (boot and CI could overlap)
  exec 9>"$LOCK_FILE"
  if ! flock -w 600 9; then
    fail "another deploy is still running"
  fi

  if [ ! -f "$DEPLOY_DIR/.env" ]; then
    fail "$DEPLOY_DIR/.env is missing"
  fi

  DOMAIN="$(grep -E '^DOMAIN=' "$DEPLOY_DIR/.env" | cut -d= -f2-)"
  if [ -z "$DOMAIN" ]; then
    fail "DOMAIN is not set in $DEPLOY_DIR/.env"
  fi

  log "Step 1/5: Pulling latest deploy files from git"
  # --ff-only refuses to merge: fails loudly if the server copy was edited
  retry git -C "$REPO_DIR" pull --ff-only || fail "git pull failed"
  log "Now at commit $(git -C "$REPO_DIR" rev-parse --short HEAD)"

  log "Step 2/5: Pulling images"
  retry compose pull || fail "docker compose pull failed"

  log "Step 3/5: Starting containers"
  compose up -d --remove-orphans || fail "docker compose up failed"

  log "Step 4/5: Health check"
  health_check || fail "health check failed"

  log "Step 5/5: Removing unused old images"
  docker image prune -f

  log "Deploy complete."
}

# Everything runs inside main(), which bash reads in full before running it.
# This matters because step 1 (git pull) can replace this very file.
main "$@"
exit
