#!/usr/bin/env bash
# Knight School - EC2 bootstrap script

set -euo pipefail   # stop on any error, unset variable, or failed pipe

# Settings
APP_USER="ubuntu"   # the user that will run docker commands
SWAP_SIZE="2G"      # swap file size
SWAP_FILE="/swapfile"
AUTO_REBOOT="false" # "true" = reboot at the end if a kernel update requires it
                    # (handy for user data; leave "false" for manual runs)
LOG_FILE="/var/log/knightschool-bootstrap.log"

# Pre-flight checks
# checks to make sure that the script is being run with root level access
if [ "$(id -u)" -ne 0 ]; then
  echo "This script must run as root. Use: sudo bash bootstrap.sh"
  exit 1
fi

# Send all output to the screen AND the log file
# 2>&1 - take any stream 2 output (stderr) and output to the same place as
# stream 1 (stdout) - LOG_FILE in this case
exec > >(tee -a "$LOG_FILE") 2>&1

log() {
  echo "[bootstrap] $(date '+%Y-%m-%d %H:%M:%S') $*"
}

# shellcheck disable=SC1091  # system file, only exists on the target machine
# ensures that this bootstrap script can't attempt to be run on an image with an OS other than Ubuntu
. /etc/os-release
if [ "${ID}" != "ubuntu" ]; then
  echo "This script supports Ubuntu only (found: ${ID})."
  exit 1
fi

# Keep apt from stopping to ask questions (there's no one to answer in user data)
# Makes the following variables visible to programs this script runs
# Proceeding this block, apt-get is run, and these two lines provide the apt-get
# program access to these variables, which in this case are configs
export DEBIAN_FRONTEND=noninteractive
export NEEDRESTART_MODE=a

# Wait up to 5 minutes if another apt/dpkg process holds the lock.
# On first boot, Ubuntu's automatic updates often run at the same time.
APT_GET=(apt-get -y -o DPkg::Lock::Timeout=300
         -o Dpkg::Options::=--force-confdef
         -o Dpkg::Options::=--force-confold)

wait_for_apt() {
  local waited=0
  while fuser /var/lib/dpkg/lock-frontend /var/lib/apt/lists/lock >/dev/null 2>&1; do
    if [ "$waited" -ge 300 ]; then
      log "Gave up waiting for the apt lock after 5 minutes."
      exit 1
    fi
    log "Another apt process is running; waiting..."
    sleep 10
    waited=$((waited + 10))
  done
}

log "Starting bootstrap on $(hostname) (${PRETTY_NAME})"

# 1. System update
log "Step 1/5: Updating system packages"
wait_for_apt
"${APT_GET[@]}" update
"${APT_GET[@]}" upgrade

# 2. Swap file
log "Step 2/5: Configuring ${SWAP_SIZE} swap"

if swapon --show=NAME --noheadings | grep -qx "$SWAP_FILE"; then
  log "Swap already active - skipping"
else
  if [ ! -f "$SWAP_FILE" ]; then
    fallocate -l "$SWAP_SIZE" "$SWAP_FILE"
  fi
  chmod 600 "$SWAP_FILE" # read, write, execute for owner
  mkswap "$SWAP_FILE"
  swapon "$SWAP_FILE"
fi

# Make swap survive reboots (Learner Lab restarts the instance every session)
# Checks if the defined /swapfile is defined in the fstab file
# If yes, nothing happens
# If no, /swapfile is added to fstab
if ! grep -q "^${SWAP_FILE} " /etc/fstab; then
  echo "${SWAP_FILE} none swap sw 0 0" >> /etc/fstab
fi

# 3. Docker Engine + Compose plugin
log "Step 3/5: Installing Docker"
if command -v docker >/dev/null 2>&1; then
  log "Docker already installed - skipping"
else
  wait_for_apt
  "${APT_GET[@]}" install ca-certificates curl # list of trusted certificates for curl to verify HTTPS sites

  # Docker's signing key, so apt can verify the packages are genuine
  install -m 0755 -d /etc/apt/keyrings # owner full access, group and user - read only access
  curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
  chmod a+r /etc/apt/keyrings/docker.asc # all users can read

  # Docker's apt repository for this Ubuntu release
  echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu ${UBUNTU_CODENAME:-$VERSION_CODENAME} stable" \
    > /etc/apt/sources.list.d/docker.list

  "${APT_GET[@]}" update
  "${APT_GET[@]}" install docker-ce docker-ce-cli containerd.io \
                          docker-buildx-plugin docker-compose-plugin
fi

# Start Docker now and on every boot, so containers return after lab restarts
systemctl enable --now containerd docker

# 4. Docker access for the app user
log "Step 4/5: Adding '${APP_USER}' to the docker group"
if id "$APP_USER" >/dev/null 2>&1; then
  usermod -aG docker "$APP_USER"
else
  log "WARNING: user '${APP_USER}' does not exist - skipped"
fi

# 5. Verify
log "Step 5/5: Verifying"
docker --version
docker compose version
docker run --rm hello-world >/dev/null
log "Docker test container ran OK"
free -h
echo "docker service at boot: $(systemctl is-enabled docker)"

# Reboot if a kernel update needs it
if [ -f /var/run/reboot-required ]; then
  if [ "$AUTO_REBOOT" = "true" ]; then
    log "Bootstrap complete. Rebooting to finish the kernel update..."
    reboot
  else
    log "Bootstrap complete. A reboot is required: run 'sudo reboot'."
  fi
else
  log "Bootstrap complete. No reboot needed."
fi

log "Log out and back in for the docker group change to take effect."
