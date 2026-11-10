# FORMA Living Coach delivery

2026-11-10. Extends the existing WebXR app; no new app, Unity implementation or paid AI service. Audit: [LIVING_COACH_AUDIT.md](../LIVING_COACH_AUDIT.md).

## A. Actually built

**FORMA START** is a data-defined 300-second equipment-free standing session: 30-second welcome/safety, 60-second march, 60-second shoulder circles, 60-second alternating knee raises, 60-second gentle standing mobility, 30-second cooldown. The first eight seconds of each main movement are its demonstration, included in the interval. Mandatory explicit feedback gates provide untimed rest between intervals; pauses and feedback extend wall time beyond five minutes.

The existing shared C# workout clock remains authoritative. Its configurable introduction defaults to the old ten seconds for previous routines and is thirty seconds here. Feedback gates hold progression, including zero-rest transitions, without assigning feedback to the next exercise. Pause freezes progression; reconfirming space alone does not resume. Skip, discomfort stop and early exit remain explicit. Standing-only eligibility respects seated setup/space and structured movement exclusions.

The existing rig now plays thirteen actual skeletal clips through Three.js AnimationMixer: idle, welcome, demonstrate_march, march_loop, demonstrate_shoulder_circles, shoulder_circles_loop, demonstrate_knee_raise, knee_raise_loop, demonstrate_mobility, mobility_loop, rest, cooldown and celebrate. Tracks articulate thighs, knees, shoulders, arms, torso and foot controls. A 300 ms weight blend smooths transitions; no whole-avatar spin substitutes for exercise. Render sampling follows the authoritative interval time, with bounded interpolation between snapshots. Pause freezes the sampled pose; feedback rest relaxes it. Reduced-motion mode uses a quiet pose and written guidance.

The model remains 1.75 m tall in its neutral pose. Immersive placement uses local-floor space, 1.8 m ahead, with explicit recentering followed by pause and renewed consent. This is a virtual placement convention, not a measurement or guarantee of available physical space. The desktop camera is closer; during the fixed session the decorative glass shell, side panel and floor timeline are hidden. Current exercise, large remaining time, next exercise, captions, pause, skip and end controls remain visible.

Optional speech uses the browser Web Speech synthesis API after an explicit button gesture. It selects only voices reported as localService for the selected TR/EN language, deduplicates instructions, cancels on interruption/mute and shows blocked/unsupported/no-local-voice status with captions. There is no microphone, voice recognition, paid API or remote TTS fallback. The adapters follow the official [Three.js AnimationMixer](https://threejs.org/docs/pages/AnimationMixer.html) and [Web Speech synthesis](https://developer.mozilla.org/en-US/docs/Web/API/SpeechSynthesis) interfaces. Captions are the same instruction passed to synthesis; word-level lip sync is not implemented. In-world speech controls and caption/timer surfaces are provided.

Live capability reporting uses XRFrame viewer and wrist joint poses where present. Desktop reports no tracking. Full-body tracking and heart rate stay false. The session is timed and user-confirmed; it never becomes sensor-assisted merely because head/hand poses are available. AR and VR remain separate explicit entries; optional hand-tracking is requested, and existing WebXR target-ray select events support available controller/hand input. Gaze dwell is not implemented.

## B. Important files

All paths are repository-relative. The historical Unity directory contains shared C# source linked into .NET; no Editor was used.

- `Unity/Assets/Forma/Product/LivingCoachWorkout.cs`: fixed program data, exact dose and compatibility validation.
- `Unity/Assets/Forma/Domain/Workout.cs`: configurable welcome duration; unchanged default for legacy clients.
- `Unity/Assets/Forma/Product/PersonalWorkout.cs`: knee-raise definition, shared engine zero-rest feedback identity, workout/provenance/duration fields, partial fixed-session outcome.
- `Unity/Assets/Forma/Product/AdaptiveTraining.cs`: keeps knee raises exclusive to the reviewed fixed program rather than silently changing existing adaptive plans.
- `tools/WebSimulation/PersonalCoachSimulation.cs`: fixed-plan action, current/next snapshots, safety checks and existing atomic save integration.
- `tools/WebSimulation/PersonalTrainingRepository.cs`: validates added provenance and partial outcomes while preserving existing schema-1/2 records and migrations.
- `tools/WebSimulation/wwwroot/living-coach-animation.mjs`: original quaternion/position clips, UUID rig bindings, blending and skinned-foot grounding.
- `tools/WebSimulation/wwwroot/studio-scene.mjs`: existing CC0 rig loading/normalization and animation adapter.
- `tools/WebSimulation/wwwroot/living-coach.mjs`: deterministic state-to-animation mapping and actual available tracking signals.
- `tools/WebSimulation/wwwroot/coach-voice.mjs`: optional local-voice adapter, cue scripts, deduplication and cancellation.
- `tools/WebSimulation/wwwroot/coach-motion.mjs`, `coach-presentation.mjs`: reuse new supported skeletal motions in existing personal plans; unrelated squat/calf/side movements retain written fallback.
- `tools/WebSimulation/wwwroot/xr.mjs`, `xr.html`, `xr.css`: integrated session entry, minimal HUD, voice/input/capabilities, camera and test-only controls.
- `tools/WebSimulation/wwwroot/holographic-studio.mjs`: quieter session stage and clearer body material; opaque platform remains hidden in AR.
- `tools/WebSimulation/wwwroot/xr-session.mjs`: optional hand-tracking feature on explicit AR/VR requests.
- `tools/WebSimulation/wwwroot/personal-coach-ui.mjs`, `studio-copy.mjs`, `studio-flow.mjs`: terminal controls, localization and honest partial summaries.
- `tests/WebSimulation/living-coach.test.mjs`: new functional, real GLB, speech and capability tests. Existing catalogue and request tests updated for the new supported content.
- `tools/test-living-coach-restart.mjs`, `.github/workflows/domain.yml`: isolated real-host restart verification and CI inclusion.

## C. Animation source and licensing

Existing Quaternius **Woman Casual** CC0-1.0 model, provenance and SHA-256 retained in `tools/WebSimulation/wwwroot/assets/coach/PROVENANCE.json`. No new downloaded character or animation asset. Exercise curves are original procedural skeletal animation, not motion capture or stock exercise clips. Existing stock Idle supplies the neutral rest pose; unrelated fight/death/run clips are not played. Three.js retains its vendored MIT license.

Actual GLTFLoader inspection exposed two causes of the earlier weak motion: dots are sanitized in bone names (`UpperLegL`, not `UpperLeg.L`), and FootL/FootR are separate exported IK controls. The adapter binds tracks by real UUID and generates foot translation from FK shin endpoints so the shoe controls follow articulated legs. Ground correction samples low skinned vertices rather than moving the whole avatar as the exercise.

This is a working stylized procedural coach. It is not photorealistic, motion-captured, physiotherapist-validated or a validated representation of ideal technique. Naturalness, subtle skin deformation and headset-scale comfort still need professional/hardware review.

## D. Verified results

**Passed:**

- 74 Node/browser-module/API tests, zero failures, including all previous tests. Runner checks JavaScript syntax and performs a Release host build with zero warnings/errors.
- Fixed workout requires safety consent, has exactly 300 timer seconds and 270 movement/cooldown timer seconds, freezes on pause, preserves each zero-rest feedback identity, skips without inventing work, distinguishes partial/full/abandoned outcomes and prevents duplicate completion records.
- New tests reject seated and excluded-movement profiles. Real GLB tests bind all thirteen clips, inspect actual thigh/knee/foot/arm displacement, sample skinned geometry at the floor, check stable root x/z, crossfade continuity and pose freeze. These are sampled numerical checks, not a full biomechanical collision proof.
- Mocked speech tests verify opt-in, local-only selection, deduplication, interruption, mute and text fallback. Capability tests do not invent hand/full-body/heart-rate signals.
- Shared C# harnesses: Domain 31 assertions, Product 71, Adaptive 1,054 across 480 cases.
- Three real process-restart scripts: athlete migration/persistence; previous adaptive-memory flow; new Living Coach 300-second completion, exact athlete/workout identity, five explicit feedback entries, single saved record and reconstructed progress after restart.
- `.NET publish -c Release` produced a framework-dependent Release package. Published host launched; `/health` and animation module served successfully. This is a local .NET/WebXR package, not a deployed production backend, Unity build or APK.
- Source invariants passed. There is no TypeScript or ESLint configuration in this project; no type-check/lint run is invented.
- Desktop Chrome: profile/setup/plan/consent/start, actual speech `onstart` status, pause/reconfirm/resume, articulated exercise views, feedback gates, completed summary and history. No console errors observed during these checks. Sound output quality and voice behavior on Quest remain unverified.

**Evidence:** `evidence/` contains raw logs and actual browser screenshots: idle, march, shoulders, knees, mobility, rest and completed summary. All screenshots use a named fictional test athlete, marked test data and, where needed, manually advanced/held **test clock**. They show rendered skeletal poses, not a human workout or proof of physical tracking. An earlier real-time preview also paused correctly when rendering was interrupted; automatic catch-up is intentionally refused. No claim of an uninterrupted human five-minute exercise trial is made.

**Not verified:** physical Quest 3/3S, immersive MR composition, headset controller/hand selection, headset audio/permissions, actual scale comfort, device frame rate/thermals, full emulated XR runtime and remote GitHub CI execution. `adb devices -l` returned an empty attached-device list. No final acceptance claim is based on mocked XR tests.

## E. Run and build

From repository root, with .NET 8 SDK available:

```bash
FORMA_SIM_PORT=5097 bash tools/run-web-simulation.sh
```

Open `http://127.0.0.1:5097/xr.html`. Set `FORMA_DOTNET=/absolute/path/to/dotnet` if outside PATH. Node 22+ is needed for tests, not to serve the app.

```bash
bash tools/test-web-simulation.sh
node tools/test-athlete-restart.mjs
node tools/test-personal-coach-restart.mjs
node tools/test-living-coach-restart.mjs
```

Restart scripts use the built Release DLL. They create isolated temporary fixture stores. For deliberate visual QA only:

```bash
FORMA_SIM_PORT=5099 FORMA_SIM_FIXTURE_DATA=1 \
  FORMA_SIM_DATA=/absolute/path/to/isolated-qa-data bash tools/run-web-simulation.sh
```

Only this fixture mode exposes the developer manual clock/hold controls. They are absent from normal consumer UI, and marked test sessions are excluded from ordinary coach memory/progress.

Release package:

```bash
dotnet publish tools/WebSimulation/WebSimulation.csproj -c Release -o /tmp/forma-living-publish
FORMA_SIM_PORT=5097 dotnet /tmp/forma-living-publish/WebSimulation.dll \
  --contentRoot /tmp/forma-living-publish
```

The published package requires .NET 8 runtime. Preserve/back up the private data root before moving the deployment. The dev runner defaults to gitignored `tools/WebSimulation/.local-data`; published content-root default differs. `FORMA_SIM_DATA=/absolute/private/data/path` explicitly fixes the location. The host is loopback-only; do not expose personal data publicly. No standalone Quest-hosted offline app or cloud account is delivered. The computer/local host must remain running; internet/LLM access is not required.

## F. Meta Quest status and launch

**Physical Quest: NOT VERIFIED.** With developer mode and USB debugging enabled, connect and approve the headset, then:

```bash
adb devices
adb reverse tcp:5097 tcp:5097
```

In Quest Browser open `http://localhost:5097/xr.html`. Meta documents localhost as a development exception; production WebXR requires HTTPS. Follow [Meta remote debugging](https://developers.meta.com/vr/documentation/web/browser-remote-debugging/) and [WebXR security guidance](https://developers.meta.com/vr/documentation/web/port-vr-xr/). Never bypass browser certificate warnings.

Physical acceptance checklist, currently all unchecked:

- [ ] Real immersive AR with transparent passthrough and no opaque environment covering the room.
- [ ] Explicit VR entry only after equipment-free consent; no automatic fallback.
- [ ] 1.75 m coach scale, floor contact, comfortable 1.8 m placement/recenter within actual safe space.
- [ ] Four articulated movements and smooth transitions at device frame rate; no objectionable clipping/foot sliding.
- [ ] Controller target-ray and supported hand pinch/select; pause/skip/feedback/end/exit accessible.
- [ ] Readable current/next timer and synchronized captions in the headset.
- [ ] Local speech availability, mute, interruption and browser permission behavior.
- [ ] Pose/focus/visibility loss pauses, never auto-resumes; surroundings remain visible in MR.
- [ ] Full workout, all feedback, saved history and reload/restart persistence on the device/browser identity.
- [ ] Quest 3 and 3S performance, thermals, comfort and movement review.

## G. Remaining limitations

The procedural coach is an original rigged fallback, with small conservative movements and no clinical validation. Full collision/biomechanical correctness cannot be asserted from the sampled tests and screenshots. Seated users must use a compatible existing routine; this standing program does not silently substitute a seated movement. Squat/calf/side in older adaptive plans still lack exercise clips and clearly use written guidance.

Speech depends on browser/OS local voices; missing TR/EN voice uses captions. No lip sync, voice input, automatic technique observation, repetition counting, calories, body composition prediction, heart-rate integration or full-body tracking. No new AI/personality/chat feature was added. Feedback remains explicit and mandatory; total wall-clock experience includes those rests.

Local identity uses the existing browser cookie and local host repository; cookie removal/another computer may disconnect access. Exports and backups are user-owned. An interrupted process preserves the record as interrupted rather than falsely completing it; active-session recovery across process restart is not implemented.

The WebXR controls and conservative capability interfaces are implemented but physical hardware acceptance remains open. Production deployment/account security, standalone hosting and advanced accessibility (including gaze dwell) are separate work.

## H. Reproduce the complete session

1. Open the normal demo, create/select your local athlete profile and complete setup. Use a compatible standing configuration; no weights.
2. Choose **FORMA START · 5 dk** (English: 5 min). Review the 300-second program. Check the explicit clear-space/no-equipment consent and start.
3. Optionally select **Sesi aç / Enable voice**. If no local voice exists, use captions. The welcome lasts thirty timer seconds.
4. Watch the first eight seconds of each movement, then follow comfortably: march, shoulder circles, alternating knee raises, gentle standing mobility. Each movement lasts sixty timer seconds. No precise form or repetition measurement is claimed.
5. At each feedback gate, rest and explicitly report completed/partial/skipped and difficulty; repetitions are optional manual entry. Discomfort stops the session.
6. Pause at any point. Reconfirm space, then separately resume; use Skip or End when appropriate. XR exit pauses first.
7. Complete the thirty-second cooldown and final confirmation. The completion summary appears only after the record is saved. A skipped/partial interval yields a partial session, not a full completion.
8. Open History/Progress, reload, or restart the host with the same private data root and browser identity. The stored session and explicit feedback remain associated with that athlete.

This vertical slice is operational in desktop testing. The full Quest acceptance milestone remains open until the physical checklist is verified.
