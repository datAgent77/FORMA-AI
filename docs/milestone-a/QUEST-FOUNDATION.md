# Milestone A — create, build and validate the native foundation

**Current result: BLOCKED.** [Latest installation/recovery findings](EDITOR-RECOVERY.md) identify the newer, incomplete 6000.6.8f1 installation; do not treat that folder as a working Editor or blindly install another copy while storage is low. The checkout is authored source, not an imported Unity project. Do not start avatar/workout milestones to hide this gate.

## Install/create the real project

1. Install Unity Hub and **6000.3.27f2** with its Android Build Support, SDK/NDK Tools and OpenJDK, from the [official Unity release](https://unity.com/releases/editor/whats-new/6000.3.27f2). Use a valid Unity licence/account appropriate to your organization; this work has not accepted licensing terms or activated an Editor. The host's independent Android SDK does not replace Unity's matching module.
2. Set the actual executable, for example:

   ```bash
   export UNITY_EDITOR='/Applications/Unity/Hub/Editor/6000.3.27f2/Unity.app/Contents/MacOS/Unity'
   bash tools/bootstrap-unity.sh
   ```

   Only the Editor may create ProjectSettings/ProjectVersion, package lock, scene/prefab/material metadata. If creating into the existing source folder is refused by the Editor, create a separate **3D Built-in** project using Hub, install the reviewed packages there, and import the preserved `Unity/Assets/Forma` sources. Preserve backups; do not replace existing source with a blank project. Copy only genuinely Editor-generated project configuration into `Unity/` and reimport/save. URP is not the current source baseline (`Unlit/Color` materials are required).
3. Review Meta/Unity package terms. With Editor closed, the existing helper can merge candidate dependency requests into an actual generated manifest:

   ```bash
   FORMA_META_LICENSE_REVIEWED=1 python3 tools/prepare-native-manifest.py --include-meta
   ```

   Set that acknowledgement only after reviewing applicable terms yourself. It never creates a project/lock or accepts an EULA. Candidate pins remain Core **207.0.1** and OpenXR **1.17.0**, with Core declaring XR Hands **1.7.2**. [Official Meta version metadata](https://npm.developer.oculus.com/com.meta.xr.sdk.core/207.0.1), [OpenXR manual](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.17/manual/index.html). These are requested versions, not a proven compatibility matrix.
4. Reopen/import, resolve packages, record the **actual** installed versions from Package Manager and generated lock. Resolve all Console/API errors against the installed SDK. Conditional Meta source has not compiled here; do not assume its APIs/assembly names are proven compatible.
5. Switch active target to Android; configure the Android OpenXR loader (one loader), startup initialization, Meta Quest support, Touch Controller profile and the current installed SDK's hand/passthrough features. Run OpenXR Project Validation and Meta Project Setup Tool; review/fix errors and preserve results. Use Vulkan only, minimum Android API 32 or higher if required by the resolved SDK, IL2CPP and ARM64. Use the Editor's recommended target API/Android toolchain, not an unverified host NDK.
6. Add `FORMA_META_XR` to Android scripting defines; confirm `Forma.Meta` resolves the real SDK `Oculus.VR` assembly. Stop on missing APIs instead of adding mocks to make the native build appear successful.

## Generate the A-only scene

Use **FORMA > Foundation A > Create Quest Foundation Scene**. It must create `Assets/Forma/Scenes/FoundationQuest.unity` using the real Editor. It refuses to overwrite an existing file. It reuses the existing SDK bridge/pointer and creates:

- Actual SDK OVRCameraRig and passthrough underlay adapter, eye camera alpha-clear configuration.
- A small floor marker/future anchor at a manually reviewed 2.4 m position; **no coach, video, room image or dumbbell**.
- A user-confirmed floor boundary (0.8 m radius), independent of eye height. This is a visualization, not Quest boundary replacement or obstacle detection.
- Large world-space floor/space confirmation, pause/resume, recenter, ±2 cm manual floor adjustment and pause-first confirmed exit controls.

Configure the real SDK hand prefabs under left/right anchors, with correct OVRHand/OVRSkeleton/OVRMesh handedness and TrackingSpace pointer roots according to the **installed** SDK. Assign distinct left/right OVRHand components to **FORMA Native Foundation A > MetaHandPointer**. The generator deliberately does not guess private SDK prefab field names. Configure OVRManager floor-level origin, supported passthrough and Controllers + Hands settings using the current SDK setup/validation. Confirm the generated Android manifest has required XR/passthrough/hand features; no raw camera recording is implemented.

Save the actual scene/assets/settings/lock and their Editor-generated `.meta` files. For Editor-only checks, **Create Labelled Editor Simulation** creates `FoundationPreview.unity` with explicit simulation text; its platform gate cannot report real readiness in a player. It is not an MR demonstration or acceptance result.

## Tests/build

```bash
bash tools/test-unity.sh
UNITY_TEST_PLATFORM=PlayMode bash tools/test-unity.sh
bash tools/build-native-foundation.sh
```

Unity tests have not run on the authoring host. The build wrapper inventories the requested FoundationQuest scene, not the later full StudioQuest. The Editor then checks loader/graphics/rig/cameras/manual placement/UI/pause and hand wiring. These checks do not substitute for SDK Project Validation or hardware testing.

Actual success must produce `Unity/Builds/FORMA-Foundation.apk` and `Unity/TestResults/native-foundation-build.json` with `buildInvoked:true`, `result:Succeeded`, the real scene/package versions and APK SHA-256. App id `com.forma.foundation.dev` intentionally identifies a development foundation, separate from the studio/session app. Do not report success from a wrapper exit before the Editor runs.

## Install on an authorized Quest 3 / 3S

Enable Developer Mode and USB debugging through Meta's device setup, connect/authorize USB. Then, using the matching Android platform tools:

```bash
adb devices
adb install -r Unity/Builds/FORMA-Foundation.apk
adb shell monkey -p com.forma.foundation.dev -c android.intent.category.LAUNCHER 1
adb logcat -s Unity AndroidRuntime
```

Record device model/OS, commit, actual package lock and APK hash. No device was installed/tested in this delivery.

## A device gate — NOT TESTED on either model

- [ ] Real room remains visible through setup, pause, floor adjustment, recenter and exit confirmation; no opaque floor/photo/skybox covers it. Initialization failure blocks readiness.
- [ ] Floor marker aligns with the user's real floor after explicit review/adjustment, both seated and standing; UI legible and controls comfortably selectable. Looking straight down cannot invent placement.
- [ ] Boundary is manually checked against actual people/pets/obstacles and Quest boundary. It is not presented as detected safe geometry.
- [ ] Left/right Touch rays select correct targets, gold hover feedback is visible, trigger holds do not repeat, B/Y pauses; loss/recovery never auto-resumes.
- [ ] Real hand rays/pinch, low confidence, occlusion, system gestures, controller/hand switching and tracking recovery behave correctly. Prefab handedness/pointer origin verified.
- [ ] Tracking loss invalidates floor and space. Focus/input/passthrough loss clears space. Recovery requires required confirmations and explicit Resume.
- [ ] Manual recenter/position change clears prior confirmations. OS-level recenter and local-origin changes are tested; no stable physical anchor persistence is claimed.
- [ ] Exit pauses first, remains in passthrough and quits only on explicit confirmation. System exit remains available.
- [ ] Actual frame timing, allocations, memory/thermal behavior and compositor stability are captured; no numerical performance claim precedes measurement.

Finish A by reconciling import/API/configuration issues, running Unity tests, generating a real APK and recording both-model results. **Stop for review before Milestone B.**
