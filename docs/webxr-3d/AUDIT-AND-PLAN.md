# WebXR first milestone — audit and plan

## Repository audit before implementation (2026-11-05)

- Active renderer: `tools/WebSimulation/wwwroot/xr.mjs`, Three.js WebGL with real immersive-ar request in `xr-session.mjs`. Desktop is not passthrough; Quest remains unverified.
- Active coach before this milestone: `video-coach.mjs`, prerecorded alpha video. `assets/studio/coach-packed.mp4` is a short looping plane, not a real-time humanoid.
- Active room before this milestone: `xr.css` uses `assets/studio/room-photo.png`, a static generated photograph.
- Available unused real 3D: `studio-scene.mjs` contains geometric studio and GLTF rig loader; `assets/coach/woman-casual.glb` has CC0 Quaternius provenance in `assets/coach/PROVENANCE.json`. Procedural bone movements are temporary demonstrations, not reviewed production animation clips.
- Working functionality: `Simulation.cs` / `DailySimulation.cs` reuse C# `WorkoutMachine`, profile constraints, recommendation engine and disk persistence. `studio-flow.mjs` handles presentation only. API tests use labelled synthetic time/signals and isolated profiles.
- Existing starters are shorter than five minutes. No actual repetition/form detection or headset tracking validation exists. No public athlete fixture is needed.

## Implementation plan and scope

1. Preserve the existing page/API and make its geometric studio and rigged humanoid the active presentation. Keep photo/video assets archived, unused on this page.
2. Use one shared C# five-minute standing program: 10-second introduction, six 30-second work sets, five 22-second rests = exactly 300 seconds. Respect existing movement exclusions, seated constraints and reported concerns. Keep previous shorter recommendation options intact.
3. Derive procedural coach pose from the server interval clock; smooth transitions to rest. Freeze motion on pause, focus loss, offline state and reduced-motion preference.
4. Verify timing boundaries, pause/resume, completion persistence and desktop loading. Document headset checks and missing professional motion assets.

Stop after this milestone. No Unity work, extra environments, dashboard expansion or LLM integration.
