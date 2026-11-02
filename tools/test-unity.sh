#!/usr/bin/env bash
set -euo pipefail
: "${UNITY_EDITOR:?Set UNITY_EDITOR to the verified Unity executable}"
repo="$(cd "$(dirname "$0")/.." && pwd)"
mkdir -p "$repo/TestResults"
test_platform="${UNITY_TEST_PLATFORM:-EditMode}"
case "$test_platform" in EditMode|PlayMode) ;; *) echo "UNITY_TEST_PLATFORM must be EditMode or PlayMode" >&2; exit 1;; esac
# Do not add -quit: Unity Test Framework exits after its test run.
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$repo/Unity" -runTests -testPlatform "$test_platform" \
  -testResults "$repo/TestResults/$test_platform.xml" -logFile "$repo/TestResults/$test_platform.log"
python3 "$repo/tools/check-test-results.py" "$repo/TestResults/$test_platform.xml"
