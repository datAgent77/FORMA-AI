# File-by-file native implementation report

All paths relative to repository root named in STATUS.md. No listed Unity source has been semantically compiled against Unity/Meta here. `Preserved` means not rewritten for web or XR.

## Native source and assemblies

- `Unity/Assets/Forma/Domain/Coach.cs` — Preserved deterministic coach and safety gate; shared .NET tests passed.
- `Unity/Assets/Forma/Domain/Forma.Domain.asmdef` — Preserved native assembly/dependency boundary; Unity resolution unverified.
- `Unity/Assets/Forma/Domain/Records.cs` — Preserved local session/profile contracts; native JsonUtility serialization untested.
- `Unity/Assets/Forma/Domain/Workout.cs` — Preserved equipment-free workout state machine; shared .NET tests passed.
- `Unity/Assets/Forma/Editor/Forma.Editor.asmdef` — Preserved native assembly/dependency boundary; Unity resolution unverified.
- `Unity/Assets/Forma/Editor/NativeQuestBuild.cs` — NEW real-Editor preconditions and Android BuildPipeline/report/hash; not invoked here.
- `Unity/Assets/Forma/Editor/StudioSceneBuilder.cs` — CHANGED rig tracking-space/floor references, hand adapter attachment, leg dimensions, temporary label; generator not executed.
- `Unity/Assets/Forma/Integrations/Meta/Forma.Meta.asmdef` — Preserved native assembly/dependency boundary; Unity resolution unverified.
- `Unity/Assets/Forma/Integrations/Meta/MetaHandPointer.cs` — NEW OVRHand confidence/pointer/pinch adapter; requires manually wired SDK prefabs and current API compilation.
- `Unity/Assets/Forma/Integrations/Meta/MetaStudioPlatform.cs` — CHANGED floor-level request, missing manager/rig gate; actual underlay/camera SDK calls remain uncompiled.
- `Unity/Assets/Forma/Product/CoachCatalog.cs` — Preserved shared product/profile/rule source; .NET harnesses passed, Unity import unverified.
- `Unity/Assets/Forma/Product/DailyExperience.cs` — Preserved shared product/profile/rule source; .NET harnesses passed, Unity import unverified.
- `Unity/Assets/Forma/Product/ExerciseCatalog.cs` — Preserved shared product/profile/rule source; .NET harnesses passed, Unity import unverified.
- `Unity/Assets/Forma/Product/Forma.Product.asmdef` — Preserved native assembly/dependency boundary; Unity resolution unverified.
- `Unity/Assets/Forma/Product/OnboardingFlow.cs` — Preserved shared product/profile/rule source; .NET harnesses passed, Unity import unverified.
- `Unity/Assets/Forma/Product/PointerActivationLatch.cs` — NEW engine-free release-before-activation guard; nine .NET assertions passed.
- `Unity/Assets/Forma/Product/ProductProfile.cs` — Preserved shared product/profile/rule source; .NET harnesses passed, Unity import unverified.
- `Unity/Assets/Forma/Product/ProductServices.cs` — Preserved shared product/profile/rule source; .NET harnesses passed, Unity import unverified.
- `Unity/Assets/Forma/Product/RecordedHistory.cs` — Preserved shared product/profile/rule source; .NET harnesses passed, Unity import unverified.
- `Unity/Assets/Forma/Product/SetupCopy.cs` — Preserved shared product/profile/rule source; .NET harnesses passed, Unity import unverified.
- `Unity/Assets/Forma/Resources/Forma/Coaches.json` — Preserved data/source asset; Unity import unverified.
- `Unity/Assets/Forma/Runtime/CoachAppearance.cs` — CHANGED explicit floor origin, metre scale, eye-height-independent placement; new Unity tests not run.
- `Unity/Assets/Forma/Runtime/CoachAvatar.cs` — Preserved procedural Unity 3D limb presentation; uncompiled, illustrative only.
- `Unity/Assets/Forma/Runtime/DailyExperienceController.cs` — Preserved native recommendation/presentation composition; Unity uncompiled.
- `Unity/Assets/Forma/Runtime/EditorPreviewPlatform.cs` — Preserved Editor-only readiness; deliberately fails player readiness.
- `Unity/Assets/Forma/Runtime/Forma.Runtime.asmdef` — Preserved native assembly/dependency boundary; Unity resolution unverified.
- `Unity/Assets/Forma/Runtime/LocalStore.cs` — Preserved JsonUtility device session adapter with atomic file replacement; not exercised by simulator.
- `Unity/Assets/Forma/Runtime/OnboardingController.cs` — Preserved native setup/profile composition; Unity uncompiled.
- `Unity/Assets/Forma/Runtime/SetupPanel.cs` — Preserved world-space Unity TextMesh/collider setup controls; Unity uncompiled.
- `Unity/Assets/Forma/Runtime/SpatialInputSource.cs` — NEW platform-independent Unity ray/press sample abstraction; syntax only.
- `Unity/Assets/Forma/Runtime/StudioButton.cs` — Preserved data/source asset; Unity import unverified.
- `Unity/Assets/Forma/Runtime/StudioController.cs` — CHANGED catch store initialization failure and block start; existing workout/local history/UI reused; Unity untested.
- `Unity/Assets/Forma/Runtime/StudioPlatform.cs` — Preserved platform boundary/readiness abstraction.
- `Unity/Assets/Forma/Runtime/StudioPointer.cs` — CHANGED controller pose rays, optional hand source, pointer-loss pause, release guard; Unity/device untested.
- `Unity/Assets/Forma/Runtime/UnityProfileCodec.cs` — Preserved data/source asset; Unity import unverified.
- `Unity/Assets/Forma/Storage/Forma.Storage.asmdef` — Preserved native assembly/dependency boundary; Unity resolution unverified.
- `Unity/Assets/Forma/Storage/ProductProfileRepository.cs` — Preserved shared product/profile/rule source; .NET harnesses passed, Unity import unverified.
- `Unity/Assets/Forma/Tests/EditMode/Forma.Tests.asmdef` — Preserved Unity test/assembly source; not run in Editor.
- `Unity/Assets/Forma/Tests/EditMode/NativePlacementTests.cs` — NEW seated/standing eye-height/floor/distance/scale tests; not run without Editor.
- `Unity/Assets/Forma/Tests/EditMode/ProductProfileTests.cs` — Preserved Unity test/assembly source; not run in Editor.
- `Unity/Assets/Forma/Tests/EditMode/StorageTests.cs` — Preserved Unity test/assembly source; not run in Editor.
- `Unity/Assets/Forma/Tests/EditMode/WorkoutTests.cs` — Preserved Unity test/assembly source; not run in Editor.
- `Unity/Assets/Forma/Tests/PlayMode/Forma.PlayModeTests.asmdef` — Preserved Unity test/assembly source; not run in Editor.
- `Unity/Assets/Forma/Tests/PlayMode/SetupSmokeTests.cs` — Preserved Unity test/assembly source; not run in Editor.

## Tooling, configuration and evidence

- `tools/native-preflight.py` — NEW read-only Editor/project/package/scene/Android inventory; exit 2 means blocked, no build invoked.
- `tools/prepare-native-manifest.py` — NEW merge recipe into an actual Editor-created project, preserve conflicting pins for review, no fabricated lock.
- `tools/build-native-quest.sh` — NEW preflight then actual batch Editor BuildPipeline entry; executed here and blocked before Unity.
- `tools/bootstrap-unity.sh`, `tools/test-unity.sh` — preserved actual-Editor creation/test entry points; cannot run here.
- `config/unity-manifest.native.json` — NEW developer recipe; not the real Unity manifest.
- `config/packages.required.json` — CHANGED candidate Core 207.0.1/OpenXR 1.17.0 registry pins and transitive Hands declaration; no installed-version claim.
- `tests/ProductHarness/Program.cs` — CHANGED nine behavioral latch assertions; 71 total passed.
- `AGENTS.md`, `README.md`, `docs/SETUP.md` — native product priority and links to current audit/build/install evidence.
- `.gitignore` — ignore actual Editor creation log; generated scenes/metadata are not ignored.
- `docs/native/AUDIT-BEFORE.md` — pre-edit 10-question evidence baseline.
- `docs/native/STATUS.md`, `FILE-REPORT.md`, `QUEST-LAUNCH.md`, `BUILD-RESULTS.md` — current status, inventory, installation, limitations, physical checklist and honest test/build results.
- `docs/native/evidence/preflight-before.json`, `preflight-after.json`, `build-attempt.txt`, `dependency-metadata.json`, `test-output.txt` — captured inventory, blocked wrapper output, official package metadata and executed test summaries.

## No serialized native scene or prefab yet

`Unity/Assets/Forma/Scenes/StudioQuest.unity` and `StudioPreview.unity` are intended Editor outputs, not existing files. SDK OVR hand/rig prefabs must come from the installed licensed SDK. No .meta, ProjectVersion, package lock, scene or prefab was authored by this agent to impersonate Editor output.
