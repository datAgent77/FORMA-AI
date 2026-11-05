# WebXR workout loop — 2026-11-05

WebXR is the active development target. Unity work is paused. This delivery improves the existing photographic studio and recorded human coach demo.

## Run

Requires .NET 8 SDK; Unity is not required. From the repository root:

```sh
FORMA_SIM_PORT=5090 bash tools/run-web-simulation.sh
```

Set `FORMA_DOTNET` to the dotnet executable if it is not on PATH. Open http://127.0.0.1:5090/xr.html for desktop preview. A physical Quest must access a trusted HTTPS origin supporting WebXR; the computer's loopback URL is not a headset deployment URL. Request immersive AR through the existing controls. No VR fallback is used for MR.

## Changes by file

- `tools/WebSimulation/wwwroot/xr.html`: visible primary workout action, session summary and local history panels; removed simulated foreground dumbbell image.
- `tools/WebSimulation/wwwroot/xr.css`: primary action, summary and history presentation, responsive placement.
- `tools/WebSimulation/wwwroot/xr.mjs`: binds the visible flow to the existing C# API; explicit space confirmation, start, pause, resume, abort, new session and history. Reconnection pauses instead of automatically resuming. XR consent requires a current visible tracked pose. Completion information also appears on the world-space timer.
- `tools/WebSimulation/wwwroot/studio-flow.mjs`: testable presentation decisions and summary mapping; no separate workout clock or exercise engine.
- `tests/WebSimulation/studio-flow.test.mjs`: five additional flow/API tests, including completion recorded once, abort distinguished from completion and durable isolated history.
- `README.md`, `AGENTS.md`, `.cursor/rules/native-milestone-a.mdc`: current WebXR-first priority; previous native evidence retained.

The authoritative workout engine, interval timing, setup rules and persistence remain in the existing C# server. No C# domain behavior was replaced or duplicated in JavaScript.

## Verification

- 51 web/API tests passed, zero failures: [captured output](evidence/tests.txt).
- Source validation, JavaScript syntax check and git whitespace checks passed.
- Manual desktop QA used a separate server on port 5092 with isolated fixture data: setup rejects missing acknowledgements; space confirmation and start are separate; pause freezes elapsed time; resume requires renewed confirmation followed by an explicit resume; stop shows an aborted summary with zero completed sessions; interrupted history is shown distinctly.
- User-facing port 5090 was reloaded and checked for console errors (none observed). Desktop screenshot: [preview](preview.png).
- Completion and disk persistence were verified with synthetic API time advancement, not by claiming a real exercise session.

## Limits and next hardware checks

The room is a photographic desktop preview, not observed passthrough. The coach is an alpha video plane using a short approximately 1.2-second loop, not a volumetric avatar. Shoulder movement currently has written guidance rather than a matching demonstration. No form detection, weight tracking, spoken coach or headset validation is claimed.

On Quest 3/3S verify trusted HTTPS access, immersive AR permission and transparent rendering, life-size floor placement, readable timer, controller selection, supported hand selection, pause on tracking/focus loss, explicit resume, completion/history persistence and safe exit. These physical headset checks remain outstanding. WebXR and native milestones are not reported complete.
