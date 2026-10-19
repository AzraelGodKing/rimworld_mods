#!/usr/bin/env bash
# Pack a Workshop-safe mod zip from git (export-ignore) and inject the built DLL.
# Usage: pack_mod.sh <folder> <zip_name> <dll_name> [dest_dir]
set -euo pipefail
folder="$1"
zip_name="$2"
dll_name="$3"
dest_dir="${4:-docs/downloads}"
mkdir -p "$dest_dir"
zip_path="${dest_dir}/${zip_name}.zip"
git archive --format=zip -o "$zip_path" HEAD "$folder"
injected=0
candidates=(
  "${folder}/1.6/Assemblies/${dll_name}.dll"
  "${folder}/1.7/Assemblies/${dll_name}.dll"
)
for dll in "${candidates[@]}"; do
  if [[ -f "$dll" ]]; then
    zip -u "$zip_path" "$dll"
    injected=1
  fi
done
if [[ "$injected" -eq 0 ]]; then
  echo "Missing ${folder} assembly (${dll_name}.dll)" >&2
  exit 1
fi
echo "${zip_name}: $(du -h "$zip_path" | cut -f1) -> ${zip_path}"
