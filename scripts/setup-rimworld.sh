#!/usr/bin/env bash
# Download RimWorld from $RIMWORLD_ARCHIVE_URL (secret), extract under /opt/rimworld,
# install Harmony, and link this repo's mods into the game Mods folder.
#
# Idempotent. Safe for Cloud Agent install + start:
#   install phase: download/extract game + Harmony + env markers
#   start / --link-only: refresh /workspace mod symlinks + ModsConfig
set -euo pipefail

RW_ROOT="${RIMWORLD_ROOT:-/opt/rimworld}"
RW_DIR="${RIMWORLD_DIR:-$RW_ROOT/RimWorld}"
MODS_DIR="$RW_DIR/Mods"
CACHE_DIR="${RIMWORLD_CACHE:-/tmp/rimworld-dl}"
WORKSPACE_ROOT="${WORKSPACE_ROOT:-/workspace}"
HARMONY_URL="${HARMONY_URL:-https://github.com/pardeike/HarmonyRimWorld/releases/latest/download/HarmonyMod.zip}"

LINK_ONLY=0
BUILD_MODS=0
for arg in "$@"; do
  case "$arg" in
    --link-only) LINK_ONLY=1 ;;
    --build-mods) BUILD_MODS=1 ;;
    -h|--help)
      echo "Usage: $0 [--link-only] [--build-mods]"
      echo "  --link-only   skip download/Harmony; refresh symlinks + ModsConfig"
      echo "  --build-mods  dotnet build all workspace mod projects (Release)"
      exit 0
      ;;
  esac
done

ensure_unrar() {
  if command -v unrar >/dev/null 2>&1; then
    return 0
  fi
  if command -v apt-get >/dev/null 2>&1; then
    sudo DEBIAN_FRONTEND=noninteractive apt-get update -qq
    sudo DEBIAN_FRONTEND=noninteractive apt-get install -y -qq unrar
  else
    echo "unrar is required to extract RimWorld.rar" >&2
    exit 1
  fi
}

ensure_dirs() {
  if [[ ! -d "$RW_ROOT" ]]; then
    sudo mkdir -p "$RW_ROOT" "$CACHE_DIR"
    sudo chown -R "$(id -u):$(id -g)" "$RW_ROOT" "$CACHE_DIR"
  fi
  mkdir -p "$CACHE_DIR"
}

write_env() {
  cat > "$RW_ROOT/env.sh" <<EOF
export RIMWORLD_DIR=$RW_DIR
export RIMWORLD_MANAGED=$RW_DIR/RimWorldWin64_Data/Managed
export RIMWORLD_ROOT=$RW_ROOT
EOF
  if [[ -d /etc/profile.d ]]; then
    sudo tee /etc/profile.d/rimworld.sh >/dev/null <<EOF
export RIMWORLD_DIR=$RW_DIR
export RIMWORLD_MANAGED=$RW_DIR/RimWorldWin64_Data/Managed
export RIMWORLD_ROOT=$RW_ROOT
EOF
  fi
}

# Persist a copy outside /workspace so Cloud Agent \`start\` can re-link mods
# even when the checked-out branch does not include this script yet.
install_self_copy() {
  local self
  self="$(readlink -f "${BASH_SOURCE[0]}")"
  mkdir -p "$RW_ROOT/bin"
  cp -f "$self" "$RW_ROOT/bin/setup-rimworld.sh"
  chmod +x "$RW_ROOT/bin/setup-rimworld.sh"
}

download_and_extract_game() {
  if [[ -f "$RW_DIR/Version.txt" && -f "$RW_DIR/RimWorldWin64_Data/Managed/Assembly-CSharp.dll" ]]; then
    echo "RimWorld already present at $RW_DIR ($(tr -d '\r' < "$RW_DIR/Version.txt"))"
    return 0
  fi

  if [[ -z "${RIMWORLD_ARCHIVE_URL:-}" ]]; then
    echo "RIMWORLD_ARCHIVE_URL is not set; cannot download RimWorld." >&2
    exit 1
  fi

  ensure_unrar
  local archive="$CACHE_DIR/RimWorld.rar"
  echo "Downloading RimWorld archive..."
  curl -fsSL -o "$archive" -L --retry 4 --retry-delay 3 "$RIMWORLD_ARCHIVE_URL"
  echo "Extracting to $RW_ROOT ..."
  # Archive root is RimWorld/...
  (cd "$RW_ROOT" && unrar x -o+ -idq "$archive")
  # Drop the large rar after a successful extract to keep the snapshot smaller.
  rm -f "$archive"
  test -f "$RW_DIR/Version.txt"
  echo "RimWorld ready: $(tr -d '\r' < "$RW_DIR/Version.txt")"
}

install_harmony() {
  mkdir -p "$MODS_DIR"
  if [[ -f "$MODS_DIR/Harmony/About/About.xml" ]]; then
    echo "Harmony already installed"
    return 0
  fi
  local zip="$CACHE_DIR/HarmonyMod.zip"
  local extract="$CACHE_DIR/HarmonyExtract"
  curl -fsSL -o "$zip" -L --retry 3 "$HARMONY_URL"
  rm -rf "$extract"
  mkdir -p "$extract"
  unzip -q "$zip" -d "$extract"
  rm -rf "$MODS_DIR/Harmony"
  if [[ -d "$extract/Harmony" ]]; then
    mv "$extract/Harmony" "$MODS_DIR/Harmony"
  elif [[ -f "$extract/About/About.xml" ]]; then
    mkdir -p "$MODS_DIR/Harmony"
    mv "$extract"/* "$MODS_DIR/Harmony/"
  else
    local top
    top="$(find "$extract" -mindepth 1 -maxdepth 1 -type d | head -1)"
    mv "$top" "$MODS_DIR/Harmony"
  fi
  test -f "$MODS_DIR/Harmony/About/About.xml"
  echo "Harmony installed"
}

link_mod() {
  local src="$1" name="$2"
  local dest="$MODS_DIR/$name"
  if [[ ! -d "$src" ]]; then
    echo "skip missing mod: $src"
    return 0
  fi
  rm -rf "$dest"
  ln -sfn "$src" "$dest"
  echo "linked $name -> $src"
}

link_workspace_mods() {
  mkdir -p "$MODS_DIR"
  link_mod "$WORKSPACE_ROOT/Homesteader" Homesteader
  link_mod "$WORKSPACE_ROOT/Strata" Strata
  link_mod "$WORKSPACE_ROOT/Stormproof" Stormproof
  link_mod "$WORKSPACE_ROOT/Nemesis" Nemesis
  link_mod "$WORKSPACE_ROOT/DateNight" DateNight
  link_mod "$WORKSPACE_ROOT/LivingWorld" LivingWorld
  link_mod "$WORKSPACE_ROOT/Deep Colony" "Deep Colony"
  link_mod "$WORKSPACE_ROOT/ShiftChange" ShiftChange
}

write_mods_config() {
  local version
  version="$(tr -d '\r' < "$RW_DIR/Version.txt" 2>/dev/null || echo '1.6.4871')"
  local config_dir="$HOME/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Config"
  mkdir -p "$config_dir"
  cat > "$config_dir/ModsConfig.xml" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<ModsConfigData>
  <version>$version</version>
  <activeMods>
    <li>brrainz.harmony</li>
    <li>ludeon.rimworld</li>
    <li>ludeon.rimworld.royalty</li>
    <li>ludeon.rimworld.ideology</li>
    <li>ludeon.rimworld.biotech</li>
    <li>ludeon.rimworld.anomaly</li>
    <li>ludeon.rimworld.odyssey</li>
    <li>AzraelGodKing.Homesteader</li>
    <li>AzraelGodKing.Stormproof</li>
    <li>AzraelGodKing.Strata</li>
    <li>AzraelGodKing.Nemesis</li>
    <li>azraelgodking.livingworld</li>
    <li>azraelgodking.DeepColony</li>
    <li>azraelgodking.DateNight</li>
    <li>azraelgodking.ShiftChange</li>
  </activeMods>
  <knownExpansions>
    <li>ludeon.rimworld.royalty</li>
    <li>ludeon.rimworld.ideology</li>
    <li>ludeon.rimworld.biotech</li>
    <li>ludeon.rimworld.anomaly</li>
    <li>ludeon.rimworld.odyssey</li>
  </knownExpansions>
</ModsConfigData>
EOF
  echo "Wrote $config_dir/ModsConfig.xml"
}

build_workspace_mods() {
  if ! command -v dotnet >/dev/null 2>&1; then
    echo "dotnet not on PATH; skip --build-mods" >&2
    return 1
  fi
  local proj
  local projects=(
    "$WORKSPACE_ROOT/Homesteader/Source/Homesteader.csproj"
    "$WORKSPACE_ROOT/Strata/Source/Strata.csproj"
    "$WORKSPACE_ROOT/Stormproof/Source/Stormproof.csproj"
    "$WORKSPACE_ROOT/Nemesis/Source/Nemesis.csproj"
    "$WORKSPACE_ROOT/DateNight/Source/DateNight.csproj"
    "$WORKSPACE_ROOT/LivingWorld/Source/LivingWorld.csproj"
    "$WORKSPACE_ROOT/Deep Colony/Source/DeepColony.csproj"
    "$WORKSPACE_ROOT/ShiftChange/Source/ShiftChange.csproj"
  )
  for proj in "${projects[@]}"; do
    if [[ ! -f "$proj" ]]; then
      echo "skip missing project: $proj"
      continue
    fi
    echo "Building $proj ..."
    dotnet build "$proj" -c Release --nologo
  done
}

main() {
  ensure_dirs
  if [[ "$LINK_ONLY" -eq 0 ]]; then
    download_and_extract_game
    install_harmony
    install_self_copy
  else
    if [[ ! -f "$RW_DIR/Version.txt" ]]; then
      echo "RimWorld missing at $RW_DIR; run without --link-only first." >&2
      exit 1
    fi
  fi
  write_env
  link_workspace_mods
  write_mods_config
  if [[ "$BUILD_MODS" -eq 1 ]]; then
    build_workspace_mods
  fi
  # shellcheck disable=SC1090
  source "$RW_ROOT/env.sh"
  echo "RIMWORLD_DIR=$RIMWORLD_DIR"
}

main
