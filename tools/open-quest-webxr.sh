#!/usr/bin/env bash
set -euo pipefail
forma_adb="${FORMA_ADB:-adb}"
port="${FORMA_SIM_PORT:-5078}"
case "$port" in ''|*[!0-9]*) echo 'FORMA_SIM_PORT must be numeric.' >&2; exit 1;; esac
serial="${FORMA_QUEST_SERIAL:-}"
if [[ -z "$serial" ]]; then
  connected="$("$forma_adb" devices | awk 'NR>1 && $2=="device" {print $1}')"
  count="$(printf '%s\n' "$connected" | awk 'NF {n++} END {print n+0}')"
  if [[ "$count" != 1 ]]; then echo 'Connect and authorize one Quest over USB, or select FORMA_QUEST_SERIAL explicitly.' >&2; exit 1; fi
  serial="$connected"
fi
model="$("$forma_adb" -s "$serial" shell getprop ro.product.model | tr -d '\r')"
case "$model" in *Quest*) ;; *) echo 'Selected device is not identified as a Quest; refusing automatic browser launch.' >&2; exit 1;; esac
"$forma_adb" -s "$serial" reverse "tcp:$port" "tcp:$port"
"$forma_adb" -s "$serial" shell am start -a android.intent.action.VIEW -d "http://127.0.0.1:$port/xr.html" >/dev/null
printf 'Opened WebXR demo on %s. In the headset, confirm the clear equipment-free area and select passthrough entry.\n' "$model"
