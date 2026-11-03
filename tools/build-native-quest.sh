#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "$0")/.." && pwd)"
mkdir -p "$repo/TestResults"
python3 "$repo/tools/native-preflight.py" --output "$repo/TestResults/native-preflight.json"
: "${UNITY_EDITOR:?Set UNITY_EDITOR to the installed, licensed Unity executable}"
"$UNITY_EDITOR" -batchmode -quit -projectPath "$repo/Unity" -buildTarget Android \
  -executeMethod Forma.Editor.NativeQuestBuild.Build -logFile "$repo/TestResults/native-build.log"
python3 - "$repo/Unity/TestResults/native-build.json" <<'PY'
import json,sys
from pathlib import Path
p=Path(sys.argv[1]);report=json.loads(p.read_text())
assert report['buildInvoked'] and report['result']=='Succeeded' and report['apkSha256'],report
print('Unity Android build succeeded. Physical Quest validation remains required.')
PY
