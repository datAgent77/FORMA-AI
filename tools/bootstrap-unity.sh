#!/usr/bin/env bash
set -euo pipefail
: "${UNITY_EDITOR:?Set UNITY_EDITOR to the verified Unity executable}"
if [[ ! -x "$UNITY_EDITOR" ]]; then
  echo "Unity executable is missing or not executable: $UNITY_EDITOR" >&2
  exit 2
fi
case "$UNITY_EDITOR" in
  */Contents/MacOS/Unity)
    unity_bundle="${UNITY_EDITOR%/Contents/MacOS/Unity}"
    if [[ ! -f "$unity_bundle/Contents/Info.plist" ]]; then
      echo "Unity installation is incomplete: Contents/Info.plist is missing. Finish/repair the Editor installation in Unity Hub." >&2
      exit 2
    fi
    if ! /usr/bin/codesign --verify --strict "$unity_bundle"; then
      echo "Unity app signature verification failed. Repair through the official installer; do not bypass macOS validation." >&2
      exit 2
    fi
    ;;
esac
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
