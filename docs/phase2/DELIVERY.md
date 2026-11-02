# Phase 2 A/B delivery — 2026-11-02

Feature branch: `feature/phase2-product-onboarding`, based on `main` at `a822630`. No automatic merge. This is continued development of the existing repo, not a new project. No speculative AI backend, store billing or immersive world has been built.

## Milestone A — Product architecture
Implemented: pure C# product profile/validation, coach definitions, beginner catalog metadata/filtering, onboarding state rules, offline dialogue/recommendation/disabled voice alternatives, service contracts and an unwired development entitlement provider. Added a codec-injected versioned atomic profile repository. Preserved the original workout engine and schema-1 history. Architecture boundaries, decisions, audit, privacy inventory and dated store-preparation sources are recorded.

Files: `AGENTS.md`; `Unity/Assets/Forma/Product/{ProductProfile,CoachCatalog,OnboardingFlow,ExerciseCatalog,ProductServices,SetupCopy}.cs` and `Forma.Product.asmdef`; `Unity/Assets/Forma/Storage/{ProductProfileRepository.cs,Forma.Storage.asmdef}`; `Unity/Assets/Forma/Resources/Forma/{Coaches.json,README.md}`; updated Runtime/Editor/test assembly references; `tests/ProductHarness/{ProductHarness.csproj,Program.cs}`; `docs/phase2/{AUDIT,ARCHITECTURE,PRIVACY,STORE-READINESS}.md`; `docs/decisions/ADR-002-product-foundation.md`.

Tests/results: the product harness executes the actual product and file-repository code using System.Text.Json, with 62 passing assertions at final integration. These cover profile validation, copy isolation, invalid transitions, optional fields, safety/focus interruption, persistence failure/retry, seated/limitation filtering, canonical instruction/dose validation, offline alternatives, safety entitlement access, actual disk reload/overwrite, corrupt/future/missing-schema preservation, export and explicit reset. The original 31 assertions also pass unchanged. Unity's JsonUtility is not exercised by the .NET harness; separate tests were supplied for it.

Build status: plain C# harnesses compiled in Release with warnings treated as errors; the product harness uses C# 9 syntax compatibility. No Unity semantic compile or Android build.

Limitations/hardware: catalog is deliberately the same two starter movements, with only seated shoulder-roll adaptation. It is not medical suitability validation. Account, store access, purchases, sync, telemetry, tracking and ghost interfaces have no production implementations. Test entitlement tiers are not wired into the product and are not proof of purchase. SDK pins remain requests, not resolved installs.

## Milestone B — First-use setup and coach creator
Implemented source: one spatial flow (Welcome → Coach → optional Profile → Safety → Review → Studio), Back/Skip rules, EN/TR setup, seated/standing preference, mute/volume/reduced-motion settings, three original development looks and independent personalities, local profile save/retry/reset/export, returning-user handoff and manual panel/coach repositioning. Completion is committed only after save success. Interruption/repositioning invalidates clear-space confirmation. Studio still requires a fresh per-session check. No compatible movement means no start.

Files: `Unity/Assets/Forma/Runtime/{OnboardingController,SetupPanel,UnityProfileCodec,CoachAppearance}.cs`; updated `CoachAvatar.cs`, `StudioController.cs`, `StudioPointer.cs`; `Editor/StudioSceneBuilder.cs`; `Tests/EditMode/ProductProfileTests.cs`; `Tests/PlayMode/{Forma.PlayModeTests.asmdef,SetupSmokeTests.cs}`; setup/UX/readme/license documentation; CI and test scripts. Full path inventory is available in the feature-branch Git diff.

Tests/results: the same 62 product assertions include onboarding navigation, interruption, save failure and offline behavior. Added four Unity EditMode codec/catalog tests and one Unity PlayMode spatial-panel smoke test, **not run**. All 27 Unity-tree C# files were parsed for C# 9 syntax using Roslyn with Editor/Meta symbols enabled; this does not resolve Unity/Meta types or prove semantic compilation. Source invariant, JSON/resource, shell syntax and Git whitespace checks passed. CI now runs both standalone harnesses; no remote CI result is assumed before observing it.

Build status: Unity Editor/Hub unavailable in checked locations/PATH. Android tools exist, but cannot compile Unity scenes on their own. Neither scene has been generated here. No APK or successful Quest build exists.

Known limitations: the visuals are authored development implementations, not premium art or evidence of comfortable headset layout. Primitive avatars have gentle illustrative movement; no speech/listening animation or natural character-animation certification is supplied. Touch gaze/controller and editor mouse are supported by source; hands are absent. Setup is bilingual, workout cues remain English with explicit disclosure. Duration/goal preferences are stored for future planning, not used to claim personalized programming. Local profile export requires developer-assisted ADB access, not a consumer share dialog. No production Home/Worlds/Progress dashboard is fabricated.

Screenshots: none; Unity/headset execution is unavailable. No mockup is presented as a working experience.

## Evidence executed on this host
- [Original domain harness log](evidence/domain.txt): 31 assertions passed.
- [Product harness log](evidence/product.txt): 62 assertions passed, including real temporary-file storage checks.
- Total: **93 passing assertions** across two compiled .NET executables, process exit 0.
- `python3 tools/validate-source.py`: source and new product/storage dependency boundaries plus coach-resource checks passed.
- `bash -n tools/bootstrap-unity.sh tools/test-unity.sh`: passed.
- C# 9 Roslyn parse: 27 source files, syntax only; not Unity API/assembly validation.
- `git diff --check`: passed before commits.

## Hardware-dependent validation before accepting B
- [ ] Generate/import the actual Unity project and both scenes with the installed supported SDK versions recorded in a real package lock.
- [ ] Run EditMode and PlayMode tests; capture nonempty passing XML reports and Editor logs.
- [ ] Build/install on Quest 3 and Quest 3S; complete the original [validation checklist](../VALIDATION.md).
- [ ] Verify first use, returning setup, optional skip, both safety acknowledgements, no automatic exercise start and offline behavior.
- [ ] Verify Turkish glyphs/line wrapping, font readability, button target sizes, seated eye height, panel placement and coach positioning around real room obstacles.
- [ ] Verify B/Y pause during every setup/workout state, exit confirmation, headset removal, tracking/passthrough failure and no automatic resume.
- [ ] Verify saved setup after app restart, full-storage retry, corrupt/newer profile preservation, reset, local export and complete local deletion.
- [ ] Verify no-compatible-movement start block and no unintended standing exercise in seated mode.
- [ ] Verify completed session totals survive changing coach/preferences and beginning a later workout.
- [ ] Inspect actual SDK telemetry/permissions, backup behavior and frame/thermal/memory performance.

## Milestones C–H
**Not started as runtime implementations.** The user's condition for C is stable and tested A/B. Headset/UI execution is unverified, so that condition has not been met. Next C work after acceptance: warm-up/skip, invalid transition expansion, durable checkpoint restoration and localized workout cues. Zen Summit, progress comparisons, production commerce, backend and store-submission assets remain later work. Source contracts and preparation documents do not count as those implementations.

## Follow-up: web simulation testing
At the user's request, a loopback browser simulator now executes the shared C# workout/onboarding rules. See [the dated web test report](../WEB-SIMULATION.md) for 22 passing HTTP scenarios, actual browser checks and captures. This adds web-flow evidence; Unity/hardware gates above remain open. No immersive world, AI backend or billing was started.
