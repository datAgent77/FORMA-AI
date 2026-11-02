#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "$0")/.." && pwd)"
forma_dotnet="${FORMA_DOTNET:-dotnet}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
# macOS sandbox file notifications can hang default host creation. Polling is local to this process.
export DOTNET_USE_POLLING_FILE_WATCHER=1
"$forma_dotnet" build "$repo/tools/WebSimulation/WebSimulation.csproj" --configuration Release --nologo
printf 'FORMA web simulation: http://127.0.0.1:%s\n' "${FORMA_SIM_PORT:-5078}"
exec "$forma_dotnet" "$repo/tools/WebSimulation/bin/Release/net8.0/WebSimulation.dll" --contentRoot "$repo/tools/WebSimulation"
