# Living Coach audit — 2026-11-10

Audit completed before implementation. Existing feature branch `feature/personal-coach-v1`, clean working tree at `18ba4ed`. This mission extends the existing app; Unity remains paused.

| Component | Current implementation | Reusable? | Required changes |
|---|---|---|---|
| Rendering / desktop | Local Three.js ES modules, geometric Studio/Forest, WebGL + desktop composer in `tools/WebSimulation/wwwroot/xr.mjs` | Yes | Reduce decorative shell/HUD, frame articulated coach |
| Coach model | `assets/coach/woman-casual.glb`, loaded by `studio-scene.mjs`; skinned meshes with named bones, normalized to 1.75 m | Yes | Preserve existing CC0 Quaternius rig and hologram materials |
| Skeletal animation | Stock idle pose frozen by AnimationMixer; manual quaternion offsets for march/shoulders, smooth slerp. Other movements explicitly lack animation. Eleven embedded stock clips do not supply required exercises | Rig yes | Author bounded skeletal clips for all four requested movements, state crossfades, verify actual bone/skin displacement |
| Exercise engine | Shared C# `PersonalWorkout.cs` wraps `Domain/Workout.cs`; feedback gating, pause, cancellation, skip. Adaptive duration plans are not this exact workout | Yes | Data-defined 300-second fixed session, configurable introduction; correct zero-rest feedback boundaries |
| Voice | No spoken session instruction integration in current XR entry point | No adapter yet | Browser speech with explicit opt-in, cancellation, captions, mute and unsupported fallback |
| Session persistence | Versioned atomic `PersonalTrainingRepository.cs`, per-athlete disk records, explicit feedback, restart tests and progress views | Yes | Identify fixed workout, preserve partial vs completed and tracking provenance, no duplicate terminal records |
| WebXR | Explicit AR/VR requests, floor space, target rays, pose/focus loss pause; physical Quest unverified | Yes | Comfortable 1.5–2 m placement, live capability reporting, in-world essential controls |
| User profile / UI | Full local profile, TR/EN, safety setup, plan/review/feedback, existing progress | Yes | Add prominent fixed session choice; standing-only eligibility, minimal live coach view |

## Dependencies and evidence

`WebSimulation.csproj` targets .NET 8 and links existing shared Domain/Product/Storage C#. Browser dependencies and avatar are vendored with existing license notices; no paid API/package is needed. Existing scripts: `tools/run-web-simulation.sh`, `tools/test-web-simulation.sh`; Node syntax/integration tests, C# harnesses, restart tests. Previous 69-test results describe the prior revision, not this mission. No physical headset was connected in the prior verification.

## Implementation order

P0: shared fixed workout using existing clock/storage; actual articulated animation clips and deterministic presentation. P1: speech/captions, pause/skip/exit, safe placement/input and capability reporting. P2: terminal semantics/history/restart tests, visual evidence and delivery report. Do not add environments, AI chat or analytics. Desktop/synthetic and physical headset results stay separate.

## Rig inspection follow-up

Parsing the actual GLB with the vendored GLTFLoader revealed sanitized names `UpperLegL`, not the old literal `UpperLeg.L`. FootL/FootR are exported IK controls under the root, not shin children. The new adapter binds by UUID and generates foot position tracks from FK shin endpoints; rotating the thigh alone leaves the shoe controls behind. Automated tests inspect actual bone and skinned-vertex positions.

Visual plan: retain navy #080f21, ice #c1e6ff, cyan #69f4ff, white #ebfcff and existing sans-serif typography. Center the human silhouette, hide the curved decorative shell during training, retain a compact timer/next/control HUD and readable captions. No new dashboards or environments.
