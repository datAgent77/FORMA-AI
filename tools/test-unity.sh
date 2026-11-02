#!/usr/bin/env bash
set -euo pipefail
: "${UNITY_EDITOR:?Set UNITY_EDITOR to the verified Unity executable}"
repo="$(cd "$(dirname "$0")/.." && pwd)"
mkdir -p "$repo/TestResults"
# Do not add -quit: Unity Test Framework exits after its test run.
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$repo/Unity" -runTests -testPlatform EditMode \
  -testResults "$repo/TestResults/editmode.xml" -logFile "$repo/TestResults/unity.log"
python3 "$repo/tools/check-test-results.py" "$repo/TestResults/editmode.xml"
