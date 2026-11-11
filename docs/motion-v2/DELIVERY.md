# Motion Coaching Engine V2 — standing alternating knee raise

2026-11-10. Implementation in the existing WebXR application. Audit: [MOTION_AUDIT.md](../MOTION_AUDIT.md). Only `standing_alternating_knee_raise` was upgraded; its existing workout/history alias remains `knees`. No Unity work, new application, external motion pack or new exercise was added.

## Status

The production-oriented **technical vertical slice is implemented and desktop-tested**: authored whole-body motion, planted contacts, deterministic event/voice integration, transitions, diagnostics and a real two-cycle canvas recording. **Professional instructor-quality acceptance remains open.** This is manually authored stylized motion, not validated mocap or a clinically reviewed demonstration. Hands/shoulders and the low-poly character's skin/clothing still merit animation/instructor review. Physical Quest acceptance and sustained device performance are not verified. Do not describe this as a production-approved human trainer.

## Previous implementation and root cause

The old knee clip was generated at runtime from clipped sine waves applied independently to thigh, shin and arm rotations. The pelvis did not transfer weight horizontally. A lowest-skinned-vertex correction moved the avatar vertically but did not plant the supporting foot. FootL/R are separate exported IK controls, so rotating a shin alone cannot move its shoe control. Existing voice announced the interval but did not follow lift/lower/contact events. Pose-name and timer checks did not establish human body mechanics.

The existing GLB was inspected: real weighted SkinnedMeshes, pelvis/spine/neck/head, shoulder/arm/palm and hip/leg chains. Its exported foot controls are supported through explicit retargeted position tracks. Embedded idle/walk/run/jump/fight clips are not a controlled knee-raise asset. The original character was retained rather than silently substituting unrelated stock animation.

## What changed

- **ExerciseDefinition:** reviewable JSON with canonical ID, display names, intended muscle groups, beginner difficulty, no equipment, duration, starting position, phases, animation reference, cues, safety/modification notes, pause behavior and completion events.
- **Motion asset:** manually authored whole-body landmarks for preparation/weight transfer, right lift/peak/lower/recovery, then the left sequence. One complete right/left cycle is twelve seconds. Quintic interpolation provides zero velocity/acceleration at authored landmarks. An offline analytical two-bone solver bakes hip/knee and contralateral arm poses; supporting feet stay fixed in world-floor space. This is constrained offline authoring, not primary runtime sine-wave motion.
- **MotionLibrary:** validates the definition, bilateral phase/contact/event coverage, finite normalized tracks, exact compatible rest pose and real weighted rig bindings. Missing/corrupt/incorrect-rig knee assets fail explicitly. The old procedural knee placeholder is not a fallback; the fixed workout cannot start in the browser without its knee asset.
- **MotionController:** AnimationMixer/AnimationAction playback at the authoritative workout time. Current-pose snapshots allow smooth rapid transitions without orphaned active actions. Pause holds the sampled pose and transition progress; resume blends from the held pose rather than snapping after a timing correction. End/rest blends back to a planted neutral instead of holding a leg in the air. Temporary actions are uncached/disposed.
- **CoachingOrchestrator:** maps the existing session to IDLE, INTRO, DEMONSTRATE, ACTIVE, PAUSED, REST and COMPLETE. It does not create another workout timer. Motion events are `rep_start`, `knee_peak`, `foot_contact`, `rep_complete`, `side_changed`, plus lift/lower cue boundaries. These are **coach demonstration events, not athlete repetitions**.
- **Voice:** short TR/EN cues are requested from actual animation phase crossings. A bounded current-phase queue drops expired cues, cancels stale/interrupted speech, handles asynchronous callbacks and avoids replay on resume. Captions use the same phase instructions in desktop and world-space XR surfaces. Browser/OS synthesis may start late; cues can be interrupted when their phase expires. No word-level synchronization or headset audio guarantee is claimed. No technique observations are invented.
- **Diagnostics:** fixture-only controls in the existing Session controls drawer: front/side/three-quarter views, 0.5×/1×, pause/scrub, two-cycle replay, skeleton/axes, clip/source/state/phase/contact/event timestamps, actual renderer counters and frame-time samples. Inspection pauses the workout and never writes a workout from its preview clock. It is disabled in immersive mode and hidden in normal consumer mode.
- **Recording:** developer-only capture of the existing WebGL canvas, no microphone/camera/tracking. Motion waits for MediaRecorder to start; capture includes the final neutral frame. Editing/replay controls lock during capture to prevent a scrubbed recording being mislabeled as two complete cycles. Disabling inspection yields a partial recording. Downloaded WebM can be converted to MP4 without synthesizing motion.
- **Visual/resource handling:** reduced scan/circuit glare retains the blue holographic silhouette. Obsolete source materials/geometries, helpers, temporary actions and page-owned rendering resources are released. Reduced-motion mode retains written guidance and suppresses exercise playback/phase speech. No exercise-driven camera motion was introduced; camera presets are inspection-only.

The existing C# session clock, profile/history repository, explicit feedback, pause/space consent, persistence, AR/VR distinction and input lifecycle remain in place. No motion event changes an athlete's recorded repetitions, body metrics or completed-session count.

## Files and ownership

Repository-relative paths:

- `tools/WebSimulation/wwwroot/assets/motion/standing-alternating-knee-raise.source.json`: original editable exercise/whole-body key-pose data.
- `tools/motion/bake-knee-raise.mjs`: reproducible offline retarget/IK bake, exact source GLB hash check and `--check` stale-asset gate.
- `tools/WebSimulation/wwwroot/assets/motion/standing-alternating-knee-raise.clip.json`: 721 samples, 40 skeletal tracks, twelve-second reusable authored clip.
- `tools/WebSimulation/wwwroot/assets/motion/PROVENANCE.md`: original asset ownership and model/library license separation.
- `tools/WebSimulation/wwwroot/motion-library.mjs`: validated asset loading and UUID rig binding.
- `tools/WebSimulation/wwwroot/motion-controller.mjs`: explicit-time playback, snapshot transitions, pause/resume and cleanup.
- `tools/WebSimulation/wwwroot/motion-events.mjs`: phase/event cursor, bounded cue queue and coaching state adapter.
- `tools/WebSimulation/wwwroot/motion-diagnostics.mjs`: inspection/capture and developer performance counters.
- `tools/WebSimulation/wwwroot/living-coach-animation.mjs`: integration adapter; replaces only the knee motion and compatible neutral poses, retaining other legacy exercises without expanding them.
- `tools/WebSimulation/wwwroot/studio-scene.mjs`: existing CoachAvatar loading/scale/material and authored-motion availability.
- `tools/WebSimulation/wwwroot/xr.mjs`, `xr.html`, `xr.css`: integration, phase captions, start guards, inspection controls and lifecycle cleanup.
- `tools/WebSimulation/wwwroot/holographic-studio.mjs`: clearer hologram and obsolete resource disposal.
- `tests/WebSimulation/motion-v2.test.mjs`, `motion-test-rig.mjs`: real-rig mechanics, state/event/voice/asset tests.
- `tests/WebSimulation/living-coach.test.mjs`: previous motion test now loads the authored asset and samples the right-first authored peak; its old left-first sine-wave assumption was removed.
- `.github/workflows/domain.yml`: reproducible bake check before existing tests. Remote CI execution is not asserted.

## Assets and licenses

Existing **Quaternius Woman Casual**, CC0-1.0, unchanged GLB SHA-256 `99c4df0aaabd19022809facc68f5fb5f564d35df3a9893c273a31fdaacf68e96`; original source/license in `assets/coach/PROVENANCE.json`. Three.js is vendored **r187**, MIT. No external exercise animation was downloaded.

New source/clip/bake are original project work, **LicenseRef-FORMA-Project-Owned**; redistribution follows the repository owner's terms. The CC0 model does not automatically dedicate new project motion IP to the public domain. No professional mocap license or quality is claimed.

Implementation follows the existing version's APIs, checked against official [AnimationMixer](https://threejs.org/docs/pages/AnimationMixer.html), [AnimationClip](https://threejs.org/docs/pages/AnimationClip.html) and [SkeletonHelper](https://threejs.org/docs/pages/SkeletonHelper.html) documentation. No dependency upgrade/install was needed.

## Automated verification

- **79 tests passed**, zero failures: previous 74 tests plus five motion-focused tests. JavaScript syntax checks and actual .NET Release build passed, zero warnings/errors. No TypeScript/ESLint setup exists; no such result is invented.
- Authored asset rebuild is byte-for-byte reproducible; `--check` passed and is included in CI.
- Real GLB sampled across **721 frames**: both sides lift, pelvis shifts about 60 mm toward the supporting leg, no avatar root translation, bounded forward knee flexion and continuous loop endpoints.
- Maximum supporting-foot bone displacement: approximately `4.9e-11 m`; **actual weighted sole vertices** move at most approximately **0.475 mm** during contact. Lowest sampled skinned vertex is approximately **-0.698 mm** relative to the floor, within the explicitly tested 5 mm geometry tolerance. This is not a claim of mathematically zero skin penetration or a biomechanical ground-contact model.
- Maximum consecutive joint rotation step at 60 Hz: approximately **0.0254 rad / 1.45 degrees**. Authored elbow-plane changes are continuous rather than switching sign at the side change.
- Tested pause/resume, rapid transitions, terminal neutral settle, missing action, controller disposal, phase ordering, left/right alternation through two cycles, bounded event history, stale cue cancellation, asynchronous callback rejection, explicit seeks and missing/corrupt/wrong-rig assets.
- Existing Living Coach real-process restart script passed: stable athlete/workout identity, one saved 300-second fixture, five explicit feedback entries, single completion and reconstructed history/progress after restart. Synthetic clock only; no human workout/tracking claim.
- Source invariants and .NET Release publish passed. This is a local WebXR/.NET package, not a Unity or Android build.

Raw logs and video metadata are under `evidence/`. An initial overlapping build attempt collided on the generated macOS apphost; rerunning builds sequentially resolved it. Final results above come from successful sequential runs.

## Visual evidence and limits

Inspected actual browser renders from front, side and three-quarter angles; both peak poses, support/weight transfer, recovery, loop boundary, skeleton/axes and neutral settle. Actual canvas recordings show two right/left cycles; MP4 files are encodings of browser-recorded WebM, not generated footage or a replacement for the live coach. See `two-cycles-three-quarter.mp4` for the final arm-continuity revision. Side/front peak images and the final three-quarter recording cover the inspected views.

Recordings are silent by design. TR phase cue requests and local speech callback behavior were checked in Chrome; the voice test additionally simulates delayed callbacks/cancellation. Exact audible timing and pronunciation on Quest remain unverified. No fake human/profile/motion measurements were added to progress.

Frame time/draw-call counters were observed in desktop diagnostics, with variable frame times during automation/recording. These are samples, not a sustained performance benchmark. Video capture has a variable encoded frame cadence; encoded video frame counts are not headset FPS. Skeleton/axes and recording add overhead. No Quest FPS/comfort claim is made.

Remaining quality limits: stylized hands/clothing, deliberately restrained authored poses, fixed cadence, flat-floor assumption and no validated human center-of-mass/balance model. There is no full-body athlete tracking, automatic repetitions, form scoring, calories or physiology inference. Professional motion-quality acceptance remains open; no additional exercise expansion was started.

## Run and evaluate

Normal app, .NET 8 SDK:

```bash
FORMA_SIM_PORT=5097 bash tools/run-web-simulation.sh
```

Open `http://127.0.0.1:5097/xr.html`, finish profile/setup, choose FORMA START, explicitly confirm clear space. Its existing knee interval uses the new motion. Voice is opt-in. Pause requires renewed space confirmation and explicit resume; skip/end remain available. History is saved through the existing repository.

Developer inspection in an **isolated** fixture root:

```bash
FORMA_SIM_PORT=5102 FORMA_SIM_FIXTURE_DATA=1 \
FORMA_SIM_DATA=/absolute/path/to/isolated-motion-qa \
bash tools/run-web-simulation.sh
```

Open `http://127.0.0.1:5102/xr.html` → Session controls / Seans kontrolleri → Enable knee-raise inspection. No athlete profile is required for inspection. Select Front, Side and Three-quarter; scrub to **2.8 s** (right peak) and **8.8 s** (left peak). Try Skeleton/Joint axes, 0.5× and 1×, pause and resume. Disable inspection to restore the camera and return to normal session controls. No inspection time is recorded as training.

For reproducible two-cycle capture: choose an angle and 1×, turn off helpers for a clean view, press **Record two cycles · Canvas only**, keep the tab visible until the completion message, then download. The motion clock covers 0–24 seconds, two full right/left cycles. Review support contact, knee direction, arms, pelvis, loop and final neutral pose. Repeat from another angle and at 0.5×. If canvas recording is unsupported, use an OS/browser screen recorder on this same playback workflow and disclose that capture method; do not substitute a rendered still.

```bash
node tools/motion/bake-knee-raise.mjs --check
bash tools/test-web-simulation.sh
node tools/test-living-coach-restart.mjs
dotnet publish tools/WebSimulation/WebSimulation.csproj -c Release -o /tmp/forma-motion-v2
```

Set `FORMA_DOTNET=/absolute/path/to/dotnet` when outside PATH. Run .NET build/publish steps sequentially. Existing local private data stays gitignored; never point fixture mode at an actual athlete's data root.

## Meta Quest — NOT VERIFIED

`adb devices -l` showed no attached headset. Existing human scale (1.75 m), floor-space placement (1.8 m), explicit AR/VR entry, controller target rays and optional hand tracking are preserved, not physically validated by this work. Diagnostics never moves the immersive camera.

With developer mode/USB debugging configured, run `adb reverse tcp:5097 tcp:5097`, then open `http://localhost:5097/xr.html` in Quest Browser. Follow the existing [Quest launch checklist](../living-coach/DELIVERY.md). Verify real passthrough visibility, floor/scale, all movement phases, ray/pinch controls, pose/focus pause, audio cancellation, comfort and sustained frame time separately on Quest 3 and 3S. These acceptance checks remain open.
