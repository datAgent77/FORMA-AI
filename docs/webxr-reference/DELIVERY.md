# Reference-led WebXR studio demo — 2026-11-03

The user explicitly authorized WebXR demos and supplied a visual direction: a human coach in the user's room, a yellow timer arc and an exercise strip. This demo work is within that scope; native Unity remains the commercial target and its milestone is still incomplete.

## What changed

- `tools/WebSimulation/wwwroot/studio-scene.mjs`: real locally hosted rigged human GLB, normalized to 1.75 m with its feet on the floor, standing pose and small procedural march/shoulder demonstrations. A wood/glass room is authored from geometry for desktop preview only. The existing AR entry hides this entire room, keeps transparent rendering and never falls back to opaque VR.
- `tools/WebSimulation/wwwroot/assets/coach/`: **Woman Casual by Quaternius**, CC0-1.0. Original distribution page, download, hash and license URL in `PROVENANCE.json`. This is a temporary stylized human, not the reference's photorealistic trainer or a scanned person. No unrelated stock run/fight/death animation is played.
- `tools/WebSimulation/wwwroot/vendor/three/{GLTFLoader,BufferGeometryUtils,SkeletonUtils}.js`: matching 0.187.0 MIT modules, imports rewritten to local paths, hashes recorded. All runtime assets stay local.
- `xr.html`, `xr.css`, `xr.mjs`: coach-centred composition, yellow countdown/body illustration, real 3D pause and session controls, data-driven exercise strip, responsive desktop controls and a short setup panel using existing C# onboarding actions. Safety boxes require explicit user selection. Full coach/profile preferences remain on the existing main page.
- `Unity/Assets/Forma/Domain/Workout.cs`: exposes the current interval duration, including a paused interval, from the same engine that drives time. The existing 10-second intro becomes a named constant without changing timing/state behavior.
- `tools/WebSimulation/Simulation.cs`: snapshot exposes actual interval duration and selected plan/doses; the ring and strip do not create a second workout engine in JavaScript.
- `tools/WebSimulation/Program.cs`: explicitly serves GLB as `model/gltf-binary`.
- `tests/WebSimulation/webxr.test.mjs`: GLB/skin/local asset checks and shared timer intro/work/pause/rest integration coverage.

## Run and use

```bash
FORMA_SIM_PORT=5090 bash tools/run-web-simulation.sh
```

Use `FORMA_DOTNET=/path/to/dotnet` if the SDK is not on PATH. The live preview for this delivery is [localhost:5090/xr.html](http://127.0.0.1:5090/xr.html). Port 5078 was already occupied by an older host and was left intact; do not use that host to verify the new snapshot fields.

Open **Kurulum**, progress through setup, review/confirm the actual safety checks, save, then choose a session and confirm clear space before Start. Pause is always explicit; loss of focus/visibility/tracking does not auto-resume. Desktop Space pauses. Moving the coach pauses and invalidates clear-space confirmation. Session history stays in the existing local development store, isolated from native profiles.

For a USB-connected authorized Quest:

```bash
FORMA_SIM_PORT=5090 bash tools/open-quest-webxr.sh
```

An actual supported Quest Browser is required for `immersive-ar`. Confirm an equipment-free clear space in the headset. No headset was connected/tested for this delivery. The reference's dumbbells, accurate hand/equipment positioning and body/muscle measurement are **not implemented**. The body icon is explicitly an illustration, not measured activation. Do not exercise with weights while testing this demo.

## Executed validation

- Web/API/AR-guard regression suite: **44 tests passed**, 0 failures, using a fresh isolated test host/data directory. Includes new GLB skin/content-type/local module checks, interval duration and pause/rest checks. Existing readiness/onboarding/recommendation safety tests retained.
- Actual shared C# DomainHarness: **31 assertions passed**.
- Actual shared C# AdaptiveHarness: **1,054 assertions passed**, 480 recommendation cases.
- Browser: model load and rendering observed; setup drawer opened, Turkish Welcome → Coach transition verified, drawer closed. Desktop screenshot captured in `preview.png` (narrow in-app viewport). No safety acknowledgement was automatically selected in the user-facing browser.
- No Unity compilation, APK build or hardware passthrough/controller/hand-tracking validation was performed. Frame rate, comfort, anatomical demonstration correctness and both-device acceptance remain unverified.

Remaining visual work for the reference's quality level: a licensed production-quality coach and exercise animation set or an explicitly chosen volumetric/video trainer pipeline, higher-fidelity environment preview and headset-driven layout/readability tuning. The present asset is a genuine 3D human mesh, but **not photorealistic**. This delivery must not be presented as matching the reference's final character quality.
