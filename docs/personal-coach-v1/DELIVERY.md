# Personal Coach V1 delivery — 2026-11-10

WebXR-first implementation in the existing FORMA Studio. Unity development remains paused. This report supersedes earlier milestone descriptions where behavior differs. Overall V1 remains **partially verified**, pending physical Quest testing and exercise animation work.

## Milestone status

- **A implemented, desktop/API verified:** full athlete profile, stable identity, versioned atomic persistence, legacy migration, conflict handling, export and explicit deletion. Optional body values are voluntary entries.
- **B implemented, desktop/API verified:** six equipment-free exercise definitions, warm-up/main/rest/cooldown, explicit feedback gates, pause/resume, skipped/partial/completed/cancelled outcomes, manual optional repetitions. Time elapsed never implies repetitions or correct form.
- **C implemented, automated verified:** beginner, consistent, returning, high-difficulty and discomfort rules; persisted evidence-backed decisions. Three eligible easy sessions increase one interval from 20 to 25 seconds. Repeated recommendation requests cannot compound the dose. Discomfort stops recommendations; no diagnosis or automatic clearance.
- **D implemented, desktop/API verified:** actual daily/weekly/monthly history, distinct completed days, work timer and known full timer separately, eight-week trends, planned interval comparison, dated measurements with provenance. UTC calendar. No invented physiological improvement or calories. Legacy missing durations remain unknown.
- **E partial:** six synchronized coach presentation states, current/next movement and timer, existing rigged 3D hologram. March and shoulder motion are temporary procedural approximations, not validated demonstrations. Squat, calf raise, side step and gentle mobility use explicitly identified written guidance. The inspected GLB's eleven clips do not provide suitable exercise demonstrations. Production motion assets remain required.
- **F desktop implemented, headset verification pending:** Studio plus original geometric Forest, pre-session selection preserves athlete/history/recommendation. Explicit AR and equipment-free VR entry paths; no automatic MR-to-VR fallback. Physical passthrough, controller/hand interaction and performance are unverified.

## File-by-file implementation map

Paths below are repository-relative. Historical `Unity/Assets` paths contain shared C# source linked by the .NET host; they do not imply a Unity build.

- `Unity/Assets/Forma/Product/AthleteProfile.cs`: validated athlete identity, goals, limits, preferences and units.
- `Unity/Assets/Forma/Product/PersonalWorkout.cs`: exercise catalogue, feedback and personal session engine wrapping existing `Domain/Workout.cs`.
- `Unity/Assets/Forma/Product/AdaptiveTraining.cs`: history memory, bounded deterministic rules, persistent evidence identifiers and plan identity.
- `Unity/Assets/Forma/Product/ProgressAnalytics.cs`: UTC period aggregation, dated measurements, provenance and actual planned-dose history.
- `Unity/Assets/Forma/Domain/Workout.cs`: explicit skip without invented work completion.
- `Unity/Assets/Forma/Domain/Coach.cs`: explicit equipment-free virtual readiness, retaining physical safety requirements.
- `Unity/Assets/Forma/Domain/Records.cs`: isolated fixture-data marker.
- `tools/WebSimulation/AthleteRepository.cs`, `PersonalTrainingRepository.cs`: versioned validated atomic JSON persistence and migrations.
- `tools/WebSimulation/LocalDataLocation.cs`: durable default root and non-overwriting legacy import.
- `tools/WebSimulation/AthleteSimulation.cs`: profile actions, concurrency, export/deletion and recovery behavior.
- `tools/WebSimulation/PersonalCoachSimulation.cs`: authoritative workout actions, recommendations, feedback, measurements, environment and retryable persistence.
- `tools/WebSimulation/Simulation.cs`, `Program.cs`: legacy compatibility, corrupt-history preservation, local cookie identity, API routing and same-origin protection.
- `tools/WebSimulation/wwwroot/athlete.mjs`, `athlete-copy.mjs`, `athlete-units.mjs`: profile UI, TR/EN copy and unit conversion.
- `tools/WebSimulation/wwwroot/personal-coach-ui.mjs`: plan review, safety consent and explicit feedback UI.
- `tools/WebSimulation/wwwroot/progress-ui.mjs`: actual-data dashboard and measurement graphs.
- `tools/WebSimulation/wwwroot/coach-presentation.mjs`: six presentation states and disclosed animation fallbacks.
- `tools/WebSimulation/wwwroot/forest-environment.mjs`, `environment-manager.mjs`: original 3D outdoor geometry and environment switching.
- `tools/WebSimulation/wwwroot/studio-copy.mjs`, `studio-scene.mjs`, `xr.mjs`, `xr-session.mjs`, `xr.html`, `xr.css`: integrated localization, rig presentation, XR/session controls and responsive panels in the existing app.
- `tests/WebSimulation/athlete.test.mjs`, `personal-coach.test.mjs`, `studio-flow.test.mjs`, `webxr.test.mjs`: persistence, failure recovery, adaptation, analytics, presentation and XR request guards.
- `tools/test-athlete-restart.mjs`, `test-personal-coach-restart.mjs`: isolated real-host restart verification.
- `.github/workflows/domain.yml`: restart verification added to CI; remote CI success is not asserted.
- `AGENTS.md`, `.cursor/rules/native-milestone-a.mdc`: current WebXR-first scope and truthful validation rules.

## Verification performed

- Release .NET host build succeeded with **0 warnings / 0 errors**. This is not a Unity or Android build.
- **69 web/domain integration tests passed**, zero failures. Runner syntax-checks top-level browser JS modules. No TypeScript or ESLint configuration exists; no such run is claimed.
- Actual isolated host restarts verified profile migration, first-session recognition, three-session adaptation, same plan in Forest, fourth completion, two dated measurements, analytics reconstruction, exclusion of marked fixture data and deletion persistence.
- Shared C# harnesses passed: Domain 31 assertions; Product 71; Adaptive 1,054 across 480 cases; Foundation 52. Foundation checks are .NET tests, not native/headset acceptance.
- Five native-preflight tool fixtures passed; no Unity Editor was run. Source invariant checks passed.
- Desktop manual checks: create/edit/reload profile, reviewed plan, feedback with partial outcome and blank repetitions, pause/cancel, progress periods, fictional dated weights and graph persistence, actual geometric Forest. Screenshots in `evidence/` contain clearly marked **test data**. Synthetic timer tests are not human workouts.
- `adb devices -l` showed no attached device. Neither physical Quest nor full emulated XR runtime has been tested. Mocked XR request guards do not establish tracking, passthrough or hand input functionality.

See raw logs in `evidence/`. A–C passed before D–F development, recorded in `MILESTONES-BC.md`.

## Run locally

Requires .NET 8 SDK; Node is needed for automated tests. Existing Three.js and avatar assets remain local; no new SDK/package installation is required.

```bash
FORMA_SIM_PORT=5097 bash tools/run-web-simulation.sh
```

Open `http://127.0.0.1:5097/xr.html`. Set `FORMA_DOTNET=/absolute/path/to/dotnet` if needed. Create a profile, complete safety setup, review Plan, explicitly confirm space, then start. Enter feedback after each interval. Review Progress after completion and reopen the browser to check remembered history.

```bash
bash tools/test-web-simulation.sh
node tools/test-athlete-restart.mjs
node tools/test-personal-coach-restart.mjs
```

Restart scripts use the built Release DLL; set `FORMA_DOTNET` if outside PATH. Their temporary fixture roots never seed normal user history. `FORMA_SIM_FIXTURE_DATA=1` is exclusively for fictional QA. Do not enable it for ordinary use.

Default records are under `tools/WebSimulation/.local-data` (gitignored), per local browser identity. Export is available from Profile. Back up exported data before moving machines. Clearing the browser identity cookie may disconnect access to that identity; this is not a cloud account system. No microphone, camera-derived measurements, LLM or external analytics service is used. Offline means no internet/AI dependency while the local host remains running; this is not a standalone installed headset application.

## Launch on Quest 3 / 3S and validate

Enable device developer mode and USB debugging through Meta's documented setup, connect the headset and accept its debugging prompt. With the local host running:

```bash
adb devices
adb reverse tcp:5097 tcp:5097
```

Open `http://localhost:5097/xr.html` in Quest Browser. Meta documents localhost as a development secure-context exception; production WebXR requires HTTPS. Do not bypass certificate warnings or publicly expose this personal-data host. Official references: [Meta remote debugging](https://developers.meta.com/vr/documentation/web/browser-remote-debugging/), [Meta WebXR migration/security](https://developers.meta.com/vr/documentation/web/port-vr-xr/), [Android localhost forwarding](https://developer.android.com/develop/ui/views/layout/webapps/access-local-server).

Physical acceptance checklist, all currently unchecked:

- [ ] Profile/setup/plan entry usable inside Quest Browser.
- [ ] Explicit MR entry preserves real-world visibility; opaque virtual environment hidden.
- [ ] Explicit equipment-free VR entry shows selected Studio/Forest; never switches automatically.
- [ ] Floor alignment, approximately life-size coach scale and comfortable placement.
- [ ] Controller ray/select and supported hand ray/select, all feedback actions accessible.
- [ ] Timer and movement instructions readable at actual headset resolution.
- [ ] Pause on tracking/focus/visibility loss; no automatic unsafe resume.
- [ ] Manual pause/resume, discomfort stop, skip, cancellation and safe XR exit.
- [ ] Full guided workout, feedback, completion and local persistence after host/browser restart.
- [ ] Stable frame time, thermals and text/transparent material performance on both devices.

## Remaining work and limitations

Priority: physical Quest validation and fixes, suitable licensed exercise animations with movement review, then headset-friendly profile/measurement input and recovery UX. Four movements currently lack animation; none is validated full-body form detection. The coach is a temporary stylized 3D hologram, not a photorealistic recorded trainer. Forest is procedural low-poly geometry, not a photorealistic scene. Existing CC0 Quaternius rig and existing library license notices remain; Forest adds no third-party asset.

Session elapsed excludes pauses/feedback waits. Legacy full durations are unknown. Optional profile weight is not silently converted into dated measurement history. Measurements remain self-reported/unverified; estimates are labelled separately. Discomfort currently conservatively blocks further recommendations without an implemented review/clearance workflow. UTC calendar may differ from the user's local training day.

Longer-term work: more reliable standalone/offline hosting, identity recovery, localization/accessibility review, reviewed motion assets and eventually a separate native Unity client consuming shared domain rules. No native APK, cloud account, body tracking, automatic repetitions, precise equipment recognition or AI coach is delivered here.
