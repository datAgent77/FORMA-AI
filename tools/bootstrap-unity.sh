#!/usr/bin/env bash
set -euo pipefail
: "${UNITY_EDITOR:?Set UNITY_EDITOR to the verified Unity executable}"
repo="$(cd "$(dirname "$0")/.." && pwd)"
target="$repo/Unity"
if [[ -e "$target/ProjectSettings/ProjectVersion.txt" ]]; then
  echo "Unity project already exists. Use Unity Hub to open it; bootstrap will not overwrite." >&2
  exit 1
fi
# Only the actual Editor generates project metadata. No package installation here.
"$UNITY_EDITOR" -batchmode -quit -createProject "$target" -logFile "$repo/unity-create.log"
test -f "$target/ProjectSettings/ProjectVersion.txt"
echo "Project created by Unity. Install reviewed packages and follow docs/SETUP.md."
