#!/usr/bin/env bash
# Idempotent RimWorld game install for Cloud Agents.
#
# Expects secret/env RIMWORLD_ARCHIVE_URL (direct HTTPS URL or Google Drive
# share link). Downloads once into /opt/rimworld, then wires this workspace's
# mod folders into the game Mods directory via symlinks.
#
# Never prints or commits the archive URL.
set -euo pipefail

RIMWORLD_ROOT="${RIMWORLD_ROOT:-/opt/rimworld}"
STAGING_DIR="${RIMWORLD_STAGING:-/opt/rimworld-staging}"
MARKER="${RIMWORLD_ROOT}/.rimworld_installed"
REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"

log() { printf '[install-rimworld] %s\n' "$*"; }
die() { printf '[install-rimworld] ERROR: %s\n' "$*" >&2; exit 1; }

ensure_dirs() {
  if [[ ! -d "$RIMWORLD_ROOT" ]] || [[ ! -w "$RIMWORLD_ROOT" ]]; then
    sudo mkdir -p "$RIMWORLD_ROOT" "$STAGING_DIR"
    sudo chown "$(id -u):$(id -g)" "$RIMWORLD_ROOT" "$STAGING_DIR"
  else
    mkdir -p "$STAGING_DIR"
  fi
}

# Return 0 if $RIMWORLD_ROOT already looks like a usable RimWorld install.
is_installed() {
  [[ -f "$MARKER" ]] || return 1
  # Linux native layout or Wine-friendly Windows layout.
  [[ -x "$RIMWORLD_ROOT/RimWorldLinux" ]] \
    || [[ -f "$RIMWORLD_ROOT/RimWorldWin64.exe" ]] \
    || [[ -f "$RIMWORLD_ROOT/RimWorld.exe" ]] \
    || [[ -d "$RIMWORLD_ROOT/Data" && -d "$RIMWORLD_ROOT/Managed" ]] \
    || [[ -d "$RIMWORLD_ROOT/RimWorldLinux_Data" ]]
}

download_archive() {
  local url="$1"
  local out="$2"
  mkdir -p "$(dirname "$out")"

  if [[ -f "$out" && -s "$out" ]]; then
    log "Archive already present at staging path ($(du -h "$out" | cut -f1))."
    return 0
  fi

  # Google Drive share / view / uc links → gdown by file id.
  local file_id=""
  if [[ "$url" =~ drive\.google\.com ]]; then
    if [[ "$url" =~ /file/d/([^/]+) ]]; then
      file_id="${BASH_REMATCH[1]}"
    elif [[ "$url" =~ id=([A-Za-z0-9_-]+) ]]; then
      file_id="${BASH_REMATCH[1]}"
    fi
  fi

  if [[ -n "$file_id" ]]; then
    log "Downloading Google Drive archive (id ${file_id:0:8}…)."
    if ! command -v gdown >/dev/null 2>&1; then
      python3 -m pip install --user -q gdown
      export PATH="${HOME}/.local/bin:${PATH}"
    fi
    # Prefer file id; gdown handles confirm tokens for large files when the
    # share is "Anyone with the link".
    if ! gdown "$file_id" -O "$out"; then
      die "Google Drive download failed. Share the file as 'Anyone with the link' (Viewer), or set RIMWORLD_ARCHIVE_URL to a direct HTTPS download URL (Dropbox ?dl=1, S3/R2 presigned, etc.)."
    fi
  else
    log "Downloading archive via curl."
    curl -fL --retry 3 --retry-delay 2 -o "$out" "$url" \
      || die "curl download failed for RIMWORLD_ARCHIVE_URL."
  fi

  [[ -s "$out" ]] || die "Downloaded archive is empty."
  log "Downloaded $(du -h "$out" | cut -f1)."
}

extract_archive() {
  local archive="$1"
  local dest="$2"
  local tmp
  tmp="$(mktemp -d "${STAGING_DIR}/extract.XXXXXX")"
  # shellcheck disable=SC2064
  trap "rm -rf '$tmp'" RETURN

  log "Extracting into staging…"
  case "$archive" in
    *.tar.gz|*.tgz) tar -xzf "$archive" -C "$tmp" ;;
    *.tar.xz|*.txz) tar -xJf "$archive" -C "$tmp" ;;
    *.tar.zst) tar --zstd -xf "$archive" -C "$tmp" ;;
    *.tar) tar -xf "$archive" -C "$tmp" ;;
    *.zip)
      if ! command -v unzip >/dev/null 2>&1; then
        sudo DEBIAN_FRONTEND=noninteractive apt-get install -y -qq unzip
      fi
      unzip -q "$archive" -d "$tmp"
      ;;
    *.7z)
      if ! command -v 7z >/dev/null 2>&1; then
        sudo DEBIAN_FRONTEND=noninteractive apt-get install -y -qq p7zip-full
      fi
      7z x -o"$tmp" "$archive" >/dev/null
      ;;
    *.rar)
      command -v 7z >/dev/null 2>&1 || sudo DEBIAN_FRONTEND=noninteractive apt-get install -y -qq 7zip 7zip-rar
      7z x -o"$tmp" "$archive" >/dev/null
      ;;
    *)
      # Guess by file(1).
      local kind
      kind="$(file -b "$archive" || true)"
      if echo "$kind" | grep -qi 'gzip compressed'; then
        tar -xzf "$archive" -C "$tmp"
      elif echo "$kind" | grep -qi 'Zip archive'; then
        command -v unzip >/dev/null 2>&1 || sudo DEBIAN_FRONTEND=noninteractive apt-get install -y -qq unzip
        unzip -q "$archive" -d "$tmp"
      elif echo "$kind" | grep -qi '7-zip'; then
        command -v 7z >/dev/null 2>&1 || sudo DEBIAN_FRONTEND=noninteractive apt-get install -y -qq p7zip-full
        7z x -o"$tmp" "$archive" >/dev/null
      elif echo "$kind" | grep -qi 'RAR archive'; then
        command -v 7z >/dev/null 2>&1 || sudo DEBIAN_FRONTEND=noninteractive apt-get install -y -qq 7zip 7zip-rar
        7z x -o"$tmp" "$archive" >/dev/null
      else
        die "Unrecognized archive type: $kind"
      fi
      ;;
  esac

  # If the archive contains a single top-level directory, use that as the root.
  local top_count child
  top_count="$(find "$tmp" -mindepth 1 -maxdepth 1 | wc -l)"
  child="$(find "$tmp" -mindepth 1 -maxdepth 1 -print -quit)"
  if [[ "$top_count" -eq 1 && -d "$child" ]]; then
    log "Promoting single top-level directory $(basename "$child")."
    # Replace dest contents atomically-ish.
    mkdir -p "$dest"
    find "$dest" -mindepth 1 -maxdepth 1 -exec rm -rf {} +
    # Move children rather than the directory itself (keeps dest mount/ownership).
    find "$child" -mindepth 1 -maxdepth 1 -exec mv {} "$dest/" \;
  else
    mkdir -p "$dest"
    find "$dest" -mindepth 1 -maxdepth 1 -exec rm -rf {} +
    find "$tmp" -mindepth 1 -maxdepth 1 -exec mv {} "$dest/" \;
  fi
}

install_from_url() {
  local url="${RIMWORLD_ARCHIVE_URL:-}"
  [[ -n "$url" ]] || die "RIMWORLD_ARCHIVE_URL is not set."

  local archive="${STAGING_DIR}/rimworld-archive.bin"
  download_archive "$url" "$archive"
  extract_archive "$archive" "$RIMWORLD_ROOT"

  # Drop the bulky staging archive after a successful extract to save disk.
  rm -f "$archive"

  date -u +"%Y-%m-%dT%H:%M:%SZ" > "$MARKER"
  {
    echo "root=$RIMWORLD_ROOT"
    if [[ -x "$RIMWORLD_ROOT/RimWorldLinux" ]]; then
      echo "platform=linux"
      echo "exe=$RIMWORLD_ROOT/RimWorldLinux"
    elif [[ -f "$RIMWORLD_ROOT/RimWorldWin64.exe" ]]; then
      echo "platform=windows"
      echo "exe=$RIMWORLD_ROOT/RimWorldWin64.exe"
    elif [[ -f "$RIMWORLD_ROOT/RimWorld.exe" ]]; then
      echo "platform=windows"
      echo "exe=$RIMWORLD_ROOT/RimWorld.exe"
    else
      echo "platform=unknown"
    fi
  } >> "$MARKER"
  log "Installed RimWorld under $RIMWORLD_ROOT"
}

# Symlink each repo mod folder into the game Mods directory.
wire_mods() {
  local mods_dir="$RIMWORLD_ROOT/Mods"
  mkdir -p "$mods_dir"

  local -a mod_names=(
    Homesteader
    Strata
    Stormproof
    Nemesis
    DateNight
    LivingWorld
    "Deep Colony"
    ShiftChange
  )

  local name src dest
  for name in "${mod_names[@]}"; do
    src="${REPO_ROOT}/${name}"
    dest="${mods_dir}/${name}"
    if [[ ! -d "$src" ]]; then
      log "Skip missing mod folder: $name"
      continue
    fi
    if [[ -L "$dest" || -e "$dest" ]]; then
      rm -rf "$dest"
    fi
    ln -s "$src" "$dest"
    log "Linked Mods/${name} → ${src}"
  done

  # Convenience env file for agents (no secrets).
  cat > "${RIMWORLD_ROOT}/.env.rimworld" <<EOF
RIMWORLD_ROOT=${RIMWORLD_ROOT}
RIMWORLD_MODS=${mods_dir}
EOF
  log "Wrote ${RIMWORLD_ROOT}/.env.rimworld"
}

smoke_check() {
  # Non-GUI verification: Data/Managed present and Mods links resolve.
  local ok=1
  if [[ -d "$RIMWORLD_ROOT/Data" || -d "$RIMWORLD_ROOT/RimWorldLinux_Data" ]]; then
    log "Data folder present."
  else
    log "WARN: no Data / RimWorldLinux_Data folder found."
    ok=0
  fi
  if [[ -d "$RIMWORLD_ROOT/Managed" ]] \
    || [[ -d "$RIMWORLD_ROOT/RimWorldLinux_Data/Managed" ]] \
    || find "$RIMWORLD_ROOT" -maxdepth 3 -type d -name Managed 2>/dev/null | grep -q .; then
    log "Managed assemblies present."
  else
    log "WARN: Managed folder not found (may still be a partial/Windows layout)."
    ok=0
  fi
  local link
  for link in "$RIMWORLD_ROOT/Mods"/*; do
    [[ -e "$link" ]] || { log "WARN: broken mod link $link"; ok=0; }
  done
  if [[ "$ok" -eq 1 ]]; then
    log "Smoke check passed."
  else
    log "Smoke check completed with warnings (install may still be usable)."
  fi
}

main() {
  ensure_dirs

  if [[ "${1:-}" == "wire-only" ]]; then
    is_installed || die "RimWorld not installed at $RIMWORLD_ROOT (cannot wire-only)."
    wire_mods
    smoke_check
    return 0
  fi

  if is_installed; then
    log "RimWorld already installed at $RIMWORLD_ROOT — skipping download."
  else
    if [[ -z "${RIMWORLD_ARCHIVE_URL:-}" ]]; then
      log "RIMWORLD_ARCHIVE_URL unset and no install at $RIMWORLD_ROOT — skipping game install."
      return 0
    fi
    install_from_url
  fi

  wire_mods
  smoke_check
  log "Done."
}

main "$@"
