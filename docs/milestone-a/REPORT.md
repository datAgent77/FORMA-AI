# Milestone A report — 2026-11-04

> Current continuation: an incomplete Unity 6000.6.8f1 installation is now present; real creation attempt failed before a project/log was produced. See [Editor recovery findings](EDITOR-RECOVERY.md). The initial audit/test results below are historical, not a successful current import/build.


**Overall: BLOCKED. Native Foundation A is not complete.** There is still no Editor-created Unity project, serialized native scene, resolved Meta/OpenXR installation or APK. Unity Editor/Hub is absent from checked host locations. The source preparation below must not be described as a working native MR application.

The mandatory [audit](../FORMA_NATIVE_XR_AUDIT.md) was produced before implementation changes. Baseline commit `7026878` was reported as a source seed before proposing new work. Existing C# rules, tests and browser reference assets were preserved. **Milestones B–E were not started.**

## Files/features prepared

All native Unity/Meta source in this list is **IMPLEMENTED BUT UNVERIFIED in Unity** unless qualified otherwise:

- `Unity/Assets/Forma/Product/NativeFoundationSafety.cs` — new platform-independent gate for floor/space confirmation, four readiness signals, pause, explicit resume and reposition invalidation. **PASS (.NET policy only)**. It does not detect obstacles or introduce workout scheduling/timers.
- `Runtime/ManualStudioPlacement.cs` — new floor-level rig reference plus bounded manual offset (±25 cm in 2 cm actions), 2–3 m placement range, default 2.4 m, look-ahead requirement, user boundary marker and world-space controls. No eye-height floor inference, room geometry, spatial-anchor persistence or camera movement.
- `Runtime/FoundationController.cs` — new A-only native composition and controls, including pause-first exit. Initial placement waits for readiness; failed placement cannot count as confirmed. UI changes are cached by state/notice rather than rebuilding status strings every frame. It has no workout/coach/audio/video dependencies.
- `Runtime/ISafePause.cs` — new shared pause contract. `StudioController.cs` only adds the interface; existing session engine behavior is preserved.
- `Runtime/StudioPointer.cs` — reuses the controller/optional Meta hand source and release latch, exposes availability, routes pause to the A controller or legacy studio, clears input/hover on disable. Editor mouse readiness is simulation-only; native platform checks remain required.
- `Runtime/StudioButton.cs` — gold selection feedback via a lazily allocated MaterialPropertyBlock; restores labels/material properties without cloning per-frame materials.
- `Editor/FoundationSceneBuilder.cs` — real-Editor authoring entry for FoundationQuest/FoundationPreview, reusing existing rig bridge/pointer/UI helpers. It creates a future placement marker, **not an avatar**, and imports no browser media. The generator has **NOT RUN**; scene output is **MISSING**.
- `Editor/StudioSceneBuilder.cs` — existing TextMesh/button/material authoring helpers exposed internally; optional button depth allows reuse without duplicating session logic.
- `Editor/NativeQuestBuild.cs` — A-only `BuildFoundation` path, separate scene/app id/APK/report, explicit APK mode and manifest/ARM64 IL2CPP archive checks, alpha-clear eye camera, floor/UI/pause and distinct hand-reference checks; rejects silently including the later StudioController/primitive coach. Legacy build path retained. Actual SDK feature/profile validation remains a separate required Editor step.
- `tools/build-native-foundation.sh` — A-specific real build entry; **BLOCKED before Editor invocation**, exit 2.
- `tools/native-preflight.py` — requested-scene inventory and explicit malformed config errors; cache/package samples do not count as authored scene/prefab/APK evidence. **PASS (three tooling tests)**.
- `tests/FoundationHarness/*` — tests link the actual Product policy source. `Tests/EditMode/FoundationPlacementTests.cs` adds seated/standing/manual-offset and failed-placement tests: **NOT TESTED in Unity**.
- `tests/test_native_preflight.py`, `.github/workflows/domain.yml` — tooling/policy regression checks integrated into existing offline CI. This CI is **not a Unity build**.
- `AGENTS.md`, `.cursor/rules/native-milestone-a.mdc`, `README.md` — current A-only scope, evidence labels and stop-for-review gate.
- `docs/FORMA_NATIVE_XR_AUDIT.md`, this report, `QUEST-FOUNDATION.md`, `evidence/*` — audit, exact setup/build/device steps and recorded outcomes.

## Tests executed

- **PASS:** Foundation safety harness **52 assertions**: missing readiness, floor before space, pause/resume, each lost signal, tracking-floor invalidation, no auto-resume, reposition and stale-consent rejection. [Output](evidence/foundation-tests.txt).
- **PASS:** existing DomainHarness **31 assertions**, ProductHarness **71 assertions**, AdaptiveHarness **1,054 assertions / 480 recommendation cases**. These compile the actual shared source in .NET; they do not compile Unity Runtime/Meta/Editor code. [Domain](evidence/domain-tests.txt), [Product](evidence/product-tests.txt), [Adaptive](evidence/adaptive-tests.txt).
- **PASS:** native-preflight tooling **3 tests**, including malformed input and refusal to fabricate project/lock/build results. [Output](evidence/tooling-tests.txt).
- **PASS:** existing secondary web regression suite **46 tests**, 0 failures. No web UI work was performed. [Output](evidence/web-regression.txt).
- **FAIL (initial, corrected):** first syntax-only pass caught escaped-newline errors in the new controller. They were fixed, not ignored. [Initial output](evidence/syntax-initial-failure.txt).
- **PASS (final):** C# 9 parser processed **41 source files** with Editor/Meta symbols and no syntax errors. This is **not semantic compilation against Unity/SDK**. [Final output](evidence/syntax-check.txt). Source invariants and shell syntax checks also passed.
- **NOT TESTED:** Unity EditMode/PlayMode, SDK assembly/API compatibility, serialized scene import, real XR rendering/input/floor placement, Android build/install and physical headset behavior.

No failing automated checks remain among those executed. An initial parser failure is not a failed Unity build; Unity was never invoked.

## Build/device status

- **BLOCKED:** `bash tools/build-native-foundation.sh` exited **2**, listing missing Editor, ProjectVersion, actual manifest/lock, resolved Core/OpenXR and FoundationQuest scene. **BuildPipeline was not invoked; no APK exists.** [Attempt](evidence/build-attempt.txt), [final inventory](evidence/final-inventory.json).
- **PASS (inventory only):** after network authorization, `adb devices` succeeded with an empty list. **No authorized USB device was listed.** This establishes no headset compatibility/functionality. [Output](evidence/adb-devices.txt).
- **NOT TESTED:** Quest 3 / Quest 3S installation, passthrough visibility, controller/hand behavior, frame stability, comfort, boundary/recenter handling and performance.

## Dependencies/assets/risks

**BLOCKED:** actual licensed Unity Editor + matching Android module/JDK, actual project creation/import and SDK resolution. Candidate versions remain Unity 6000.3.27f2, Core 207.0.1 and OpenXR 1.17.0. Core version metadata was re-fetched from its official registry, including declared Hands 1.7.2: [evidence](evidence/meta-package-metadata.json). No new package was installed into Unity and no EULA accepted. No installed package exists against which to verify APIs; compilation/reconciliation is still mandatory.

**MISSING:** real SDK rig/left-right hand prefab wiring in a saved scene, actual Editor-generated settings/metadata and Project Validation results. Avatar/Animator/exercise/audio assets remain missing for later milestones; no placeholder is claimed to meet that target. A adds no speculative AI, Island, recognition or form correction.

**Known limitations:** floor/boundary confirmation is manual and not obstacle detection. Placement is in local floor coordinates, not a persistent physical anchor. OS recenter/origin changes require hardware review. Native controller/hand APIs and prefab layout are uncompiled; the existing Meta adapter has not been proven compatible with the candidate pins. The build preconditions cannot prove physical passthrough visibility or hand tracking correctness.

**Performance observations:** only static source design observations are available: unlit materials, no dynamic lighting/post-processing added by the generator, fixed boundary point count, no per-frame material cloning, and state-cached UI updates. **Actual draw calls, skinning cost, GC, frame time and thermal behavior: NOT TESTED.** No benchmark numbers are reported.

## Next gate / stop

Follow [QUEST-FOUNDATION.md](QUEST-FOUNDATION.md) to install/licence the real toolchain, import/reconcile packages/APIs, configure OpenXR/Meta, generate/wire the scene, run Unity tests/build and validate Quest hardware. Commit genuine Editor outputs with their owning assets. **Stop for user review here. Do not begin Milestone B.**
