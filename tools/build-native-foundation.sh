#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "$0")/.." && pwd)"
mkdir -p "$repo/TestResults"
python3 "$repo/tools/native-preflight.py" --scene Unity/Assets/Forma/Scenes/FoundationQuest.unity --output "$repo/TestResults/foundation-preflight.json"
: "${UNITY_EDITOR:?Set UNITY_EDITOR to the installed, licensed Unity executable}"
"$UNITY_EDITOR" -batchmode -quit -projectPath "$repo/Unity" -buildTarget Android \
  -executeMethod Forma.Editor.NativeQuestBuild.BuildFoundation -logFile "$repo/TestResults/foundation-build.log"
python3 - "$repo/Unity/TestResults/native-foundation-build.json" <<'PY'
from pathlib import Path
import json,sys
report=json.loads(Path(sys.argv[1]).read_text())
assert report['buildInvoked'] and report['result']=='Succeeded' and report['apkSha256'],report
assert report['scene']=='Assets/Forma/Scenes/FoundationQuest.unity',report
print('Foundation Android build succeeded. Device acceptance remains unverified.')
PY
