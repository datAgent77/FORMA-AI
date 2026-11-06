# First 3D milestone — implementation and remaining validation

## Status

Functional source slice implemented, with automated API verification and desktop start/pause smoke test. **Not accepted as a premium finished experience:** environment art, professional exercise animations and physical Quest checks remain outstanding. No Unity installation, scene generation or APK work was performed.

[Pre-implementation audit and plan](AUDIT-AND-PLAN.md) · [Desktop screenshot](preview.png) · [Test output](evidence/web-tests.txt).

## Implemented

- Existing `xr.html` experience now renders the rigged Quaternius CC0 humanoid in `studio-scene.mjs`, scaled to 1.75 world meters with feet at floor height. License and asset hash remain in `wwwroot/assets/coach/PROVENANCE.json`.
- Original geometric studio includes timber floor/ceiling, glazing, terrace, plants, furniture and lights. `createEnvironment('studio')` is the environment boundary; unknown environments fail explicitly. Beach/forest/park are not implemented. No photo is rendered as this environment.
- `coach-motion.mjs` derives animation phase from authoritative interval seconds and remaining time. Procedural march and shoulder movements use the actual rig, with quaternion interpolation between poses. Pause, rest, offline state, focus loss and reduced motion stop the exercise cycle. These are approximate demonstrations, not validated biomechanics or licensed motion-capture exercise clips.
- `DailyWorkoutCatalog.FiveMinutePlan` in the existing shared C# Product source defines 10 seconds introduction + six 30-second work sets + five 22-second rests = exactly 300 seconds. March and shoulder movements are required; seated/excluded movements and previously reported concerns block this program. Existing shorter recommendation plans are retained.
- `DailySimulation.cs` exposes explicit `dailyFive` selection without duplicating the engine in JavaScript. `xr.mjs` uses that selection before a fresh start, preserves explicit resume and records aborted sessions before restarting. Completion and history continue through existing server-side disk storage. No repetitions are fabricated.
- `xr.html` / `xr.css`: removed active video/photo presentation; preserved visible pause, summary/history and safe exit controls; restart returns to explicit space confirmation rather than auto-starting.

Shared C# lives under the historical `Unity/Assets/Forma/Product` directory and is linked into .NET. Editing that reusable source is not native Unity development. Three.js scenes will require a separate future Unity presentation implementation.

## Verification

Run from repository root with .NET 8 SDK:

```sh
FORMA_SIM_PORT=5093 bash tools/run-web-simulation.sh
bash tools/test-web-simulation.sh
```

Use `FORMA_DOTNET=/absolute/path/to/dotnet` if needed. Open http://127.0.0.1:5093/xr.html. Complete setup, separately confirm the space, start, pause, confirm space again and explicitly resume. Use controls → Bitir for an aborted summary or Yeniden başlat for a fresh attempt. Complete all intervals for completion; history differentiates completion from interruption/abort.

53 web/API tests passed. The five-minute boundary test reaches rest at 40 seconds, rejects time advancement during pause, remains incomplete at 299 elapsed workout seconds, completes at 300 with six sets and 180 active work seconds. Existing isolated persistence tests verify disk history and prevent duplicate completions. A separate motion-policy test checks time-derived phase and pause/offline/focus/reduced-motion gates. Synthetic test clock advancement is not evidence of a human workout.

Desktop browser smoke test: actual GLB loaded, setup completed in isolated port-5093 data directory, new program started with 30-second interval and three sets, pause returned to renewed space consent. Screenshot is from that isolated test, not real athlete history. No physical movement was measured. Visual polish and animation quality require further review.

## Quest checks still required

The current server intentionally binds loopback. The desktop URL cannot be entered unchanged on another device. For headset testing, provide a trusted HTTPS reverse proxy on the development machine to this server and a headset-accessible hostname; do not disable TLS validation or expose personal profiles publicly. No remote deployment was performed here.

On Quest 3 and 3S separately check immersive-ar support, transparent real-world rendering, local-floor scale/placement, controller ray selection, supported hand selection, timer readability, focus/tracking loss pause, explicit resume, completion persistence and XR exit. The virtual room is hidden in MR to preserve the physical environment. A fully immersive virtual environment mode is not supplied by the current AR-only session launcher.

No real Quest hand tracking, passthrough quality, exercise tracking, native build or mobile profile sync has been verified. No LLM, dashboard expansion or additional environment was added. Professional licensed exercise animation clips and art direction are outstanding dependencies for the requested premium quality.
