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
dll="${folder}/Assemblies/${dll_name}.dll"
git archive --format=zip -o "$zip_path" HEAD "$folder"
if [[ ! -f "$dll" ]]; then
  echo "Missing $dll" >&2
  exit 1
fi
zip -u "$zip_path" "$dll"
echo "${zip_name}: $(du -h "$zip_path" | cut -f1) -> ${zip_path}"
