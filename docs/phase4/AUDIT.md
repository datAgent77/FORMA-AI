# Phase 4 implementation gap report — 2026-11-02

Baseline: `ee0bc0d` on `feature/phase2-product-onboarding`, clean tree, matching the fetched remote. Dedicated continuation branch: `feature/phase4-daily-experience`. No merge.

- Implemented/tested: deterministic workout/coach domain (31 assertions); profile/onboarding/catalog/file-repository rules (62 assertions); local shared-C# web simulator (22 HTTP scenarios). All re-executed before Phase 4 code edits. Initial web attempt lacked sandbox loopback permission; after permission, all 22 passed.
- Implemented/unverified in Unity: passthrough adapter, procedural avatar, spatial setup/workout panels, controller input and Unity JsonUtility/session storage. There are no actual generated Unity scenes, prefabs, .meta files, package manifest/lock or successful APK.
- Missing: daily readiness, multi-input recommendations and truthful reasons/alternatives, history-aware daily greeting, persistent complete workout-loop data shared across adapters. No Phase 3 implementation was found; a prompt number is not evidence.
- Partial: versioned local profile, interruption checkpoints, local history. No crash reconstruction of an active workout; web history was a separate JSON-array adapter, Unity used LocalStore. This duplication is a migration/integrity concern. Do not pretend that web tests execute Unity LocalStore.
- Development placeholders: original primitive Unity characters and SVG browser illustration, simulated XR readiness and clock. Hand tracking, spoken audio, recognition/form correction, AI, cloud accounts, billing, Island, progress replay, habit reminders and analytics transport are absent.
- Tooling: no Unity Editor/Hub in checked application locations/PATH. Android SDK/ADB and NDK exist but cannot build Unity alone. Requested Unity 6000.3.27f2 / Meta Core 207.0.0 / OpenXR 1.16.1 / Test Framework 1.4.6 are not installed/resolved package evidence. No new dependencies or permissions will be installed for daily rules.
- Official review: [Unity release notes](https://unity.com/releases/editor/whats-new/6000.3.27f2), [Meta Unity requirements](https://developers.meta.com/vr/documentation/unity/unity-development-requirements/), and [Movement body-tracking documentation](https://developers.meta.com/vr/documentation/unity/move-body-tracking/) were revisited. Device-specific tracking availability never establishes validated form/rep accuracy; Movement remains out of scope.

## Immediate scope
A: deterministic daily check-in, conservative allowlisted recommendation, truthful history greeting, simple recommendation card/alternatives and quick choice. Exercise budget options are ceilings, not fabricated 5–30 minute programs; the current catalog remains 65–135 second equipment-free starters. Unsupported strength requests must say so.

After verified daily behavior, B: strengthen shared session persistence, explicit save-failure/retry, guided existing workout, and history-backed summary in the browser acceptance path. Unity source integration may be authored but remains unverified. No later Island/relationship/habit milestone is considered done. Physical Quest validation is separate.
