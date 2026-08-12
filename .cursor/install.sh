#!/usr/bin/env bash
# Idempotent Cloud Agent bootstrap for the RimWorld mods monorepo.
# Installs the .NET SDK the mods compile against and warms the NuGet cache by
# building every mod. Node.js, Python, git and Chrome come from the base image.
set -euo pipefail

cd "$(dirname "$0")/.."

# .NET SDK 8 — matches the "Build mod DLLs" GitHub Action (dotnet-version 8.0.x).
if ! command -v dotnet >/dev/null 2>&1; then
  echo "[install] Installing .NET SDK 8.0..."
  sudo apt-get update -qq
  sudo DEBIAN_FRONTEND=noninteractive apt-get install -y -qq dotnet-sdk-8.0
else
  echo "[install] .NET SDK already present: $(dotnet --version)"
fi

# Skip the first-run banner / telemetry noise during automated builds.
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Build each mod in Release. This restores NuGet packages (Krafs.Rimworld.Ref,
# Lib.Harmony, reference assemblies) into ~/.nuget so later builds are offline-fast,
# and produces the gitignored Assemblies/*.dll the developer needs.
PROJECTS=(
  "Homesteader/Source/Homesteader.csproj"
  "Strata/Source/Strata.csproj"
  "Stormproof/Source/Stormproof.csproj"
  "Nemesis/Source/Nemesis.csproj"
  "DateNight/Source/DateNight.csproj"
  "LivingWorld/Source/LivingWorld.csproj"
  "Deep Colony/Source/DeepColony.csproj"
  "ShiftChange/Source/ShiftChange.csproj"
)

for proj in "${PROJECTS[@]}"; do
  echo "[install] Building $proj"
  dotnet build "$proj" -c Release --nologo
done

echo "[install] Done. Built ${#PROJECTS[@]} mod assemblies."

# Optional: download RimWorld from RIMWORLD_ARCHIVE_URL and symlink repo mods
# into /opt/rimworld/Mods. No-ops when the secret is unset and no install exists.
if [[ -x .cursor/install-rimworld.sh ]]; then
  bash .cursor/install-rimworld.sh
fi
