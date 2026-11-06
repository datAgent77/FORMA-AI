# Holographic Studio visual revision — 2026-11-06

This revision follows the user's luminous blue studio reference within the existing WebXR application. The five-minute C# workout engine and local session storage are unchanged. No Unity development or new package dependency was introduced.

## Changes

- `holographic-studio.mjs`: original geometric dark studio, ceiling/wall lights, tiled floor, curved transparent glass shell, projection lane and circular platform. Rigged avatar uses smoothed normals, translucent scan/circuit shader and a skinned wire surface. These are presentation effects, not measured anatomy or form detection. No people or physiological values are invented.
- `studio-composer.mjs`: local Three.js desktop bloom renderer using half-float render targets and separable blur. Immersive sessions bypass postprocessing; the opaque room and platform are hidden in AR. Physical Quest rendering remains unverified.
- `studio-scene.mjs`: exposes the existing avatar for material styling and uses a neutral idle pose. Existing Quaternius CC0 asset/provenance is retained; no new external model, image, video or texture was downloaded.
- `xr.mjs`: camera composition, draggable desktop viewpoint, glass workout HUD with actual timer/state/set data, integrated holographic presentation. Existing start/pause/resume/restart/history controls remain.
- `xr.css` / `xr.html`: coordinated navy/ice-blue controls, typography, overlays and narrow-screen layout. No room photograph is used.

## Run and verify

```sh
FORMA_SIM_PORT=5094 bash tools/run-web-simulation.sh
```

Requires .NET 8 (`FORMA_DOTNET` can specify the executable). Open http://127.0.0.1:5094/xr.html. Drag the background to change the viewpoint. Finish setup, separately confirm clear space, then start. Pause and resume remain explicit. Stop/restart and XR exit are in Seans kontrolleri.

53 web/API tests passed, including the 300-second session and persistence behavior inherited from the previous milestone. JavaScript syntax and source invariants passed. Desktop Chrome loaded the hologram without observed console errors. Wide and 390×844 layouts were inspected; setup, separate start confirmation, the running timer, explicit pause and drag-to-orbit were exercised with isolated port-5094 fixture data. The timer remained paused during viewpoint interaction. This test data is not an athlete's exercise record. [Captured desktop view](preview.png).

The image reference is visual direction, not a claim of photorealistic humans, validated anatomy or medical sensing. The existing avatar and approximate procedural exercise animations remain temporary. No Quest 3/3S hardware, passthrough, hand interaction or performance validation was performed. Existing hardware checklist remains in `docs/webxr-3d/DELIVERY.md`.
