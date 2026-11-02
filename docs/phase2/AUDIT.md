# Implementation audit — 2026-11-02, before Phase 2 edits

Repository: datAgent77/FORMA-AI. Local and GitHub `main` both at `a82263082d021be884105add4d934acfa88f0533`. Working tree initially clean. Phase 2 branch: `feature/phase2-product-onboarding`. No merge authorized.

## Implemented and tested
- Pure C# workout timing, selection, introduction, work/rest/set progression, pause/resume/abort, terminal behavior and deterministic coach cues: original 31 domain assertions recompiled and passed on .NET SDK 8.0.427.
- Basic source invariants and shell syntax checks passed. These do not compile Unity assemblies.

## Implemented but unverified
- Meta passthrough adapter, camera rig composition and safety readiness signals.
- Desktop/Quest scene generator, primitive coach animation, gaze/Touch input, large stop controls.
- Unity JsonUtility profile/session persistence, manual counts/load entry, summary/history UI and local deletion.
- Unity NUnit domain/persistence tests and Android development build command: authored but not run in Unity.

## Partially implemented
- Local profile has only exercise-selection flags. No first-use onboarding or coach creator.
- Interrupted sessions are checkpointed as Interrupted; no checkpoint restoration/resume after restart.
- Accessibility: controller gaze and editor mouse only; no validated hands, seated adaptation, audio cues or localized UI.
- XR safety uses initialization/tracking flags and manual confirmation; no final compositor visibility or physical-room understanding.
- CI compiles only pure C#; no licensed Unity runner or hardware gate.

## Placeholder
- `EditorPreviewPlatform` is an explicitly Editor-only readiness mock, never a headset passthrough implementation.
- Avatar uses primitive development art/illustrative motion, not a finished commercial character or validated demonstration.
- Future AI, body tracking, Worlds, Mirror, voice, equipment and analytics interfaces have no implementation.

## Not implemented
Accounts, cloud sync, purchases/subscriptions, store entitlement enforcement, AI services, voice capture/output, motion recognition/replay, immersive worlds, production onboarding, store assets and release approval.

## Tooling and dependencies
- Unity Editor / Unity Hub absent in checked `/Applications` locations and PATH. No ProjectSettings, ProjectVersion.txt, Unity scenes, metadata, resolved manifest or package lock. No installed Unity/Meta package versions can be reported.
- `config/packages.required.json` contains requested pins only: Unity 6000.3.27f2, Meta Core request 207.0.0 (release page 207.0), OpenXR 1.16.1, Test Framework 1.4.6. Not a tested compatibility matrix.
- Android SDK is present at `~/Library/Android/sdk`: build-tools 35.0.0/35.0.1/36.0.0, platforms android-35/android-36, NDK 27.1.12297006 and ADB 35.0.2. This corrects the older report's assumption that no Android tools existed; it does not establish Unity toolchain compatibility or a connected headset.
- XR interaction currently uses `UnityEngine.XR` trigger/secondary-button state, collider hit-testing and a head gaze ray. No hand-select provider, XR Interaction Toolkit or Meta Interaction SDK installation exists.

## Issues found before implementation
README test commands lost spaces (`--projecttests`, `python3tools`) and its no-remote statement is stale. The old AGENTS scope must be updated to the user's explicitly authorized Phase 2 A/B scope. Product onboarding must not imply commercial readiness while Unity/device testing is missing. Optional SDK references and all scene-generation output still need Editor import validation.

## Scope decision
Implement A (architecture/models/contracts) and B (offline first-use flow, original development coach presets, minimal optional profile and safe Phase 1 handoff). Preserve the Phase 1 engine. C is gated on A/B being stable and tested; Unity/UI execution cannot be established here, so C/D/billing/backend are deferred. See delivery report for actual results after implementation.
