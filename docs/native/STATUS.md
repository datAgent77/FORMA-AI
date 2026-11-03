# Native Unity implementation status

**INCOMPLETE — source prepared, not an imported/compiled/runnable Unity XR product.** Updated 2026-11-02. Consumer target: native Unity/C# Quest 3 / 3S. Browser/WebXR stays a secondary developer tool. No browser polish was added in this correction.

Repository root: `/Users/saita/Documents/Codex/2026-10-31/forma-ai-project-genesis-you-are/outputs/FORMA-AI`.

## Exact audit answers

The immutable [before-change audit](AUDIT-BEFORE.md) records the baseline. Current answers:

1. **Actual Unity project: no.** `Unity/Assets/Forma/` is an authored source seed. `Unity/ProjectSettings/ProjectVersion.txt`, `Unity/Packages/manifest.json`, `Unity/Packages/packages-lock.json` remain absent. [Machine-readable inventory](evidence/preflight-before.json).
2. **Scenes/prefabs: none serialized.** `Unity/Assets/Forma/Editor/StudioSceneBuilder.cs` is an Editor generator for `StudioPreview.unity` / `StudioQuest.unity`. Native C# components implement timer/UI, workout coordination, primitive coach, profile/daily flow and local history, but no Unity compile/import has verified them. [Every native source file](FILE-REPORT.md).
3. **Meta SDK: not installed/resolved.** `Unity/Assets/Forma/Integrations/Meta/Forma.Meta.asmdef` conditionally references `Oculus.VR`. `MetaStudioPlatform.cs` and new `MetaHandPointer.cs` are uncompiled adapters. Candidate package recipes exist in `config/`; they are not resolved project dependencies.
4. **Passthrough: source only.** Meta adapter enables OVR passthrough underlay, opacity 1 and transparent camera clears; generator adds the SDK layer/rig only when actually invoked in Editor. No compositor rendering or hardware visibility has been observed.
5. **3D coach: temporary 3D primitive generator only.** No native prefab exists yet. Source generates metre-scale mesh body and animated limbs, with corrected 0.92 m legs reaching the origin and floor-origin placement instead of an eye-height assumption. No HTML/SVG/Canvas is used in this native path. Production skinned avatar, validated exercise animation and spatial anchors remain absent. Actual scale/placement must be checked in Unity and headset.
6. **Can produce an APK now: no.** New `NativeQuestBuild.cs` and `tools/build-native-quest.sh` prepare a real BuildPipeline path with explicit preconditions. Missing Editor/project/packages/scene prevent invocation today.
7. **Successful build: none.** Wrapper exited 2 before Editor invocation. No Unity compilation/build/installation occurred. [Raw attempt](evidence/build-attempt.txt), [test report](BUILD-RESULTS.md).
8. **Only web:** HTTP service, HTML/CSS/SVG UI, browser Three.js/WebXR rendering and events, simulated XR faults/test clock, browser test hooks and its System.Text.Json history adapter (`tools/WebSimulation/`). Those paths never establish native readiness.
9. **Shared rules:** actual `Domain/*.cs`, `Product/*.cs` and `Storage/*.cs` are linked by web/.NET harnesses and referenced via native asmdefs; deterministic workouts, coach policy, profile/recommendation/safety exclusions and profile repository are preserved. New `Product/PointerActivationLatch.cs` also stays engine-free. Native `Runtime/LocalStore.cs` JsonUtility session persistence is distinct from simulator history serialization and still needs Unity testing. The simulator does not exercise native adapters or input.
10. **Quest blockers:** missing Editor + matching Android module/license, actual project import and package lock, SDK/API reconciliation, Android XR/manifest configuration, generated/saved native scene and assets, manual SDK hand prefab references, Unity tests, actual APK build and both-model device acceptance. No device connected via ADB. Host Android tools alone cannot satisfy these.

## Native source changes and remaining gates

- Controller pointer now consumes tracked controller poses in rig tracking space, with trigger selection and B/Y pause. The conditional Meta adapter supplies high-confidence valid hand rays/index pinch; manual SDK prefab wiring is required. Pure input latch requires release on acquisition, tracking recovery or source change, preventing held input from selecting Resume. Loss of an available pointer pauses. **Input provider compatibility and behavior are not verified in Unity.**
- Coach placement uses the floor-level rig origin, 2.6 m distance and unit scale; scene generator corrects the floating legs and labels the asset temporary. No inference of room obstacles/user body/equipment is made. Manual confirmation and Quest boundary are still required.
- Meta adapter requests floor-level origin and fails closed when manager/rig/layer are absent. Timer/work/rest, deterministic written coaching, pause/resume/completion, safe exit and local records reuse existing engine/runtime instead of duplicating in JS.
- Runtime now catches local store construction failure, keeps default profile and exit/pause UI available, and blocks starting when the store could not initialize. Writes during a session retain the existing explicit failure notice/retry behavior; durable save on full/damaged storage requires device testing.
- Build pipeline refuses missing Android/OpenXR/Vulkan/rig/hands/floor/UI prerequisites and records a real build report/hash only after Editor execution. Scripts do not fabricate Unity metadata, scenes, prefabs or a lockfile.

Next milestone requires importing and fixing the source in real Unity, resolving/licensing SDKs, generating/wiring/saving the scene, running Unity tests, producing a successful ARM64 development APK and passing the physical checklist. It is **not complete** merely because source files or .NET tests exist.

[Installation and Quest launch](QUEST-LAUNCH.md) contains the exact sequence and unchecked physical validation gates. [Build results](BUILD-RESULTS.md) separates executed .NET/parser checks from unexecuted Unity/device work. Existing GitHub CI remains a shared-rule/web check, not a Unity build.
