# Phase 4 daily experience — delivery status, 2026-11-02

Branch: `feature/phase4-daily-experience`, based on `ee0bc0d`. No automatic merge. [Initial gap report](AUDIT.md).

## Milestone A — Implemented and verified in the web acceptance path
Before: two filtered starter movements, deterministic workout/onboarding rules, setup/profile persistence and shared-C# browser testing. No daily check-in or history-aware greeting. Baseline 31 domain assertions, 62 product assertions and 22 HTTP scenarios were rerun successfully.

Implemented: daily energy/time/activity check-in, optional skip/quick choice, manually configured seated/standing area and equipment inventory, required space confirmation, conservative safety-concern block, deterministic recommendation with true duration/intensity/equipment/reasons and compatible alternatives, and truthful last-completion greeting. Existing setup remains complete after relaunch. Profile goals/preferences, experience, voluntary exclusions, recent recorded participation, return gap, capability availability and time/space limits enter explicit rules.

The catalog intentionally contains only the existing reviewed starter movements: gentle mobility (65 s), in-place steps (65 s) and the pair (135 s). High energy does not increase dose. Strength is not yet in the reviewed catalog and is never relabeled onto another session. Recent-participation thresholds (three completions in 48 h or four in seven days) are a conservative product heuristic, not a physiological load estimate. No amount of tracking permission bypasses symptom/space/catalog guards. Check-in safety concerns are in-memory only and not written into exported session/profile data.

Files: `Product/DailyExperience.cs`, `Product/RecordedHistory.cs`, `Runtime/DailyExperienceController.cs`, Studio/Onboarding/scene-builder integration, `tools/WebSimulation/DailySimulation.cs`, `wwwroot/daily.js`, existing simulation/UI adapters, `tests/AdaptiveHarness`, `tests/WebSimulation/daily.test.mjs`, CI and documentation. Paths are relative to the repo (`Product`/`Runtime` under `Unity/Assets/Forma`).

Architecture: retain WorkoutMachine and existing schemas; pure C# rules shared by web and Unity; presentation adapters do not become AI services or motion estimators. Daily context never increases the beginner allowlist/dose. History uses valid completed versioned records, removes duplicate IDs and ignores malformed/future/incomplete data. Same-plan comparisons are factual and never imply physiology. Nothing is merged into main.

Tests: 1,054 adaptive assertions across 480 recommendation matrix combinations passed. Explicit cases cover low/high energy, unsupported strength, symptom/skip conflict, unknown inputs/capabilities, seated/standing conflict, movement exclusions, participation thresholds, missing/future/duplicate/aborted history, no fabricated memories, equal-session comparisons, and coach state mapping. Fourteen new HTTP scenarios passed alongside the original 22. Browser verified Turkish low-energy/10-minute/recovery inputs produced an actual 65-second gentle card with matching reasons and the saved 80-second/four-set historical greeting.

Build: shared C#/.NET host Release build passed with zero warnings/errors. Roslyn parsed all 30 Unity-tree C# files for C# 9 syntax, not Unity semantic compilation. Unity Editor, actual scene generation, Meta/Unity API compilation and device builds remain not run.

Known limitations: native Unity daily UI is authored but unverified, and remains development art/panels. No hand integration, spoken audio or medically validated exercise programming. Native body/workout cues and some enum labels remain English. The two-minute arrival target has not been measured with beta users. No island, progression rewards, full coach-memory system, habit/reminder service, telemetry transport, AI or billing was implemented.

## Milestone B — Existing loop exercised; new reliability milestone is not declared complete
The existing guided introduction/work/rest/pause/resume/end/summary loop runs through the daily-selected plan in the browser. Actual C# workout timing and recorded completion support the new greeting. Existing JSON history persistence is still the earlier adapter; there is no new transactional session journal, exact interrupted-machine reconstruction or full save-failure recovery system. Web history and Unity LocalStore are not interchangeable test evidence.

Because Unity/device acceptance of A is still unverified and B's persistence improvements are unfinished, this report does not declare B complete and does not start later milestones. Next required work is native import/device acceptance plus shared session durability, pending-save/retry behavior and interruption recovery tests. Screenshots of WebGL are never offered as Unity/Quest proof.

## WebXR steering request
The user subsequently requested running the demo with WebXR. A genuine `immersive-ar` entry path, transparent rendering, 3D controller targets, explicit entry and pause/exit protections were added; see [WebXR report](../WEBXR.md). The browser successfully rendered WebGL and executed start/pause against the shared C# motor, but reported no immersive-ar support. ADB reported no connected Quest. Therefore no headset session or passthrough test is claimed. Combined web/guard suite: 42/42 passing, no skipped scenarios; six guard tests use explicit capability mocks.

## Evidence and remaining hardware acceptance
[Web test log](evidence/web-tests.txt), [WebXR desktop capture](evidence/webxr-preview.jpg). The desktop capture is the genuine running 3D preview and displays its unsupported-AR message.

Still required on Quest 3 and 3S separately: actual package lock and compile, real immersive-ar session/transparent compositor, controller rays and emergency pause, tracking/focus interruptions, physical room visibility/placement/seated comfort, frame/thermal/memory measurements, offline and disk-failure behavior, permission/license review and signed builds. No store approval or hardware safety certification is asserted.
