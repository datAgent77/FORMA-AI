#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "$0")/.." && pwd)"
forma_dotnet="${FORMA_DOTNET:-dotnet}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_USE_POLLING_FILE_WATCHER=1
export FORMA_SIM_PORT="${FORMA_SIM_PORT:-5079}"
export FORMA_SIM_URL="http://127.0.0.1:$FORMA_SIM_PORT"
if curl --silent --fail "$FORMA_SIM_URL/health" >/dev/null; then
  echo "Test port is already occupied. Choose another FORMA_SIM_PORT; refusing to test/reset an unrelated host." >&2
  exit 1
fi
"$forma_dotnet" build "$repo/tools/WebSimulation/WebSimulation.csproj" --configuration Release --nologo
simulation_data="$(mktemp -d "${TMPDIR:-/tmp}/forma-sim-test.XXXXXX")"
export FORMA_SIM_DATA="$simulation_data"
"$forma_dotnet" "$repo/tools/WebSimulation/bin/Release/net8.0/WebSimulation.dll" --contentRoot "$repo/tools/WebSimulation" > "$simulation_data/host.log" 2>&1 &
simulation_pid=$!
cleanup(){ kill "$simulation_pid" 2>/dev/null || true; wait "$simulation_pid" 2>/dev/null || true; rm -rf -- "$simulation_data"; }
trap cleanup EXIT
ready=false
for attempt in $(seq 1 30); do
  if curl --silent --fail "$FORMA_SIM_URL/health" >/dev/null; then ready=true; break; fi
  if ! kill -0 "$simulation_pid" 2>/dev/null; then cat "$simulation_data/host.log"; exit 1; fi
  sleep 1
done
if [[ "$ready" != true ]]; then cat "$simulation_data/host.log"; echo 'Simulator did not become ready.' >&2; exit 1; fi
node --check "$repo/tools/WebSimulation/wwwroot/app.js"
node --test "$repo/tests/WebSimulation/api.test.mjs"
