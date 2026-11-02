# ADR-002: Offline product onboarding around the existing Studio engine

Status: Accepted for implementation; Unity and device acceptance pending.
Date: 2026-11-02. Decider: FORMA engineering, within the user's Phase 2 A/B scope.

## Context
Phase 1 provides a tested deterministic domain but no generated Unity project. Adding a backend, account system or XR framework migration would increase risk before onboarding can be exercised on a headset.

## Decision
Keep Forma.Domain and its timing semantics unchanged. Add Forma.Product (plain C# models, coach definitions, onboarding state machine, capability/safety policy and service contracts), Forma.Storage (plain C# versioned profile repository with injected codec), and Unity presentation/codec adapters in Forma.Runtime. Compose dependencies explicitly; no service locator, network layer or DI container.

The onboarding flow is Welcome → Coach → Profile → Safety → Review → Complete. Optional fitness fields may be skipped; physical-space/safety acknowledgement may not. Save must succeed before Complete. Lifecycle interruptions invalidate physical confirmation. Returning users enter the Studio but still need a new per-session clear-space check. Editing setup is forbidden while a workout is running or paused; stop first.

Coach presets are original procedural development characters defined by reviewed JSON. Appearance and personality are separate user choices. Speech and listening are unavailable; text guidance never pretends to be recorded audio or live listening. Seated/lower-body/shoulder exclusions filter the fixed beginner catalog; if no compatible movement exists, start is blocked. Filtering is not medical clearance.

A separate product-profile.json schema 2 leaves Phase 1 profile.json/session schema 1 untouched. Corrupt/newer product profiles remain on disk and cannot be overwritten without an explicit reset. Data is local plaintext under the application sandbox. All elective fields have purpose labels; no names, ages, diagnosis text, microphone or motion recording is collected.

## Alternatives
- Expand StudioController to own onboarding: fewer files, but UI/lifecycle/persistence rules would become untestable without Unity. Rejected.
- Adopt cloud accounts, remote configuration and store billing now: useful eventually, but unnecessary infrastructure and identity/consent risk before core validation. Deferred.
- Replace the workout engine immediately: would invalidate 31 passing assertions before the UI is proven. Deferred to C.

## Consequences
More small assemblies, but business/persistence tests execute without a headset. Unity JsonUtility needs its own codec tests, separate from the .NET harness serializer. Original primitive characters and TextMesh panels are development implementations, not proof of premium art or headset comfort. Hands require a validated input adapter; only Touch and mouse are currently wired. No unsupported interaction is advertised.

## Follow-up gates
- Import/generate scenes and execute Unity tests, verify actual package lock and assembly compatibility.
- Validate layout, Turkish glyphs, seated heights, repositioning, controller emergency pause and passthrough on Quest 3/3S.
- Only after A/B runtime acceptance, implement C: warm-up, skip, checkpoint restoration, full exercise localization and richer structured definitions.
- Store app entitlement verification is a separate release requirement from development feature-tier mocks. Do not ship mocks as receipt validation.
