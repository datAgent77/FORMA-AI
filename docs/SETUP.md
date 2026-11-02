# Unity, packages and Quest setup

## Verified baseline — 2026-10-31
Official pages were consulted before any Unity/XR dependency installation. No Unity or proprietary package was installed on this host. The .NET SDK used for domain-only checks is independent of Unity.

- Unity **6000.3.27f2** is published in the [official release notes](https://unity.com/releases/editor/whats-new/6000.3.27f2). Select this Unity 6.3 LTS patch for the first import. [Unity support policy](https://unity.com/releases/unity-6/support) lists 6.3 LTS support through December 2027. This is a baseline choice, not a claim of device testing.
- [Meta hardware/software requirements](https://developers.meta.com/vr/documentation/unity/unity-development-requirements/) specify Unity 6000.0.66f2 or later and Android build modules. The selected LTS exceeds that minimum. Their platform details and passthrough guide differ on macOS support; validate this workflow with the installed Editor and standalone device rather than assuming Link works on macOS.
- [Meta Core SDK download/release page](https://developers.meta.com/vr/downloads/package/meta-xr-core-sdk/) lists **207.0**. Request `com.meta.xr.sdk.core` **207.0.0** through the official Unity distribution and confirm the package's exact `package.json` version after import. The marketing release number alone does not establish the resolved UPM patch spelling or cross-package compatibility. If that exact version is unavailable, do not invent a pin: inspect the official distribution, document the actual version and rerun validation.
- [Unity OpenXR 1.16 documentation](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.16/manual/index.html) identifies **1.16.1**, package `com.unity.xr.openxr`.
- [Unity Test Framework 1.4 documentation](https://docs.unity3d.com/Packages/com.unity.test-framework@1.4/manual/index.html) identifies **1.4.6**, package `com.unity.test-framework`.
- [Meta project setup](https://developers.meta.com/vr/documentation/unity/unity-project-setup/) directs new setup toward OpenXR; use OpenXR rather than the deprecated OculusXR provider. This does not prohibit `OVRCameraRig` or the Core SDK's managed `Oculus.VR` assembly.
- Meta Movement SDK is deliberately **not installed**. No body tracking is required in Phase 1. Device/OS support, license and exact SDK compatibility must be re-verified before a later tracking task.

These are independently documented versions, not a tested compatibility matrix. Let Package Manager report dependencies/conflicts and record every actual resolved version. Do not install All-in-One, Platform, Voice or Movement merely for future interfaces.

## Generate the real Unity project
1. Install Unity Hub, sign into your licensed Unity account, and install the baseline Editor with **Android Build Support, Android SDK & NDK Tools, and OpenJDK**. Use Unity's bundled toolchain for that Editor; do not guess separate NDK versions.
2. Keep a backup of these authored Assets. Create a **3D Core (Built-in Render Pipeline)** project in a temporary sibling folder through Hub, then merge its real generated project files into `Unity/`, keeping `Assets/Forma`. Alternatively, set `UNITY_EDITOR` and run `bash tools/bootstrap-unity.sh`; that invokes the Editor's actual `-createProject` command in `Unity/` and refuses an existing project. Review generated template assets/settings; remove any template sample scene from the build.
3. Open `Unity/` in the Editor and let it generate metadata and import scripts. Use the legacy Input Manager or **Both** active-input mode for desktop mouse/Space preview; the headset path uses `UnityEngine.XR` devices. If prompted, install/enable Unity XR modules supplied by the template or SDK.
4. Install Test Framework 1.4.6 via Package Manager. For desktop preview, no Meta package is required. Run `FORMA > Create Desktop Preview Scene`, open it and enter Play. Mouse clicks select buttons; Space pauses. A visible label states that the Editor is not showing physical passthrough.
5. If Unity reports an unresolved optional `Oculus.VR` assembly before Meta installation, keep the Meta integration folder outside `Assets` until step 6. Do not add fake SDK classes to make compilation pass.
6. Acquire Meta XR Core SDK through [Meta's official installation path](https://developers.meta.com/vr/documentation/unity/unity-package-manager/) and review its license. Install the verified release, verify `Oculus.VR` is the actual assembly name, then return the integration folder to Assets if removed. Install OpenXR 1.16.1. If the SDK requires a different supported OpenXR patch, document that conflict and use only a combination confirmed by official package metadata, then update `config/packages.required.json` and test it. No resolved lock is claimed in this repository.
7. Set the `FORMA_META_XR` scripting define for Android (and the Editor's active Android target) after importing Core. The adapter is isolated in its own assembly. **Do not define it before Core is present.** Compile and fix any SDK API changes against the installed release before proceeding; the adapter was authored, not compiled here.

## Configure XR and generate StudioQuest
1. Switch to Android in Build Profiles. Enable **OpenXR** in XR Plug-in Management for Android, enable the **Meta XR feature group**, and enable the **Oculus Touch Controller Profile** so Unity XR exposes trigger/secondary button input. Resolve Project Validation errors using the SDK's setup tooling. Avoid simultaneous OculusXR and OpenXR loaders.
2. Run `FORMA > Create Quest Studio Scene`. It creates a genuine scene with the SDK prefab and a passthrough underlay. Generation refuses to overwrite existing scenes. Review and save it in the Editor.
3. Follow [Meta passthrough setup](https://developers.meta.com/vr/documentation/unity/unity-passthrough-gs/): select the rig's OVRManager, set **Passthrough Support = Required** and **Enable Passthrough**. Confirm reconstructed passthrough, Underlay, opacity 1, transparent camera clear color and no skybox/opaque floor. Keep passthrough enabled during pause, history and exit confirmation. Camera permission for raw camera access is not needed by this design and must not be requested.
4. Choose a floor-relative tracking origin in the Meta rig so the coach and panels use the expected heights; use the SDK-supported Floor Level/Stage setting for the installed version. Confirm actual world positions and gaze interaction on device. Do not silently recenter during a workout. Inspect all eye cameras to ensure transparent alpha reaches the underlay.
5. Use IL2CPP and ARM64. Choose a supported graphics API through Meta Project Setup (begin with its recommended Vulkan configuration), and resolve Android API/manifest requirements for the installed SDK and current device OS rather than copying stale numeric API targets. Disable application internet access if not required by the selected packages. Inspect the merged Android manifest: passthrough required, Quest 3/3S supported device entries, no unnecessary camera/microphone/storage permissions.
6. Record actual Editor version, Meta/OpenXR package versions, toolchain, graphics API, Android min/target SDK, device OS, refresh rate and signing mode in the validation record. Commit generated Assets with their `.meta` files, real `.unity` scenes, ProjectSettings and `Packages/manifest.json` / `packages-lock.json`. Do not commit Library, personal data or signing keys.

## Quest device connection
Follow [Meta device setup](https://developers.meta.com/vr/documentation/native/android/mobile-device-setup/). Use a Meta developer account/organization with the required verification. In Meta Horizon mobile app, select the paired Quest and enable developer mode. Connect via a data-capable USB-C cable, put on the headset, approve USB debugging for this development machine and confirm the device appears in `adb devices`. Keep system boundary protection enabled and set a clear, stationary workout space. These steps are not permission to bypass device safety features.

On macOS use standalone Android deployment; do not depend on Windows-only Quest Link. Simulator/Editor interaction can assist iteration but cannot validate passthrough quality, boundary safety, input mapping or comfort on hardware.

## Tests and development APK
Run the domain harness from repository root, then Unity EditMode tests:
```sh
dotnet run --project tests/DomainHarness/DomainHarness.csproj --configuration Release
UNITY_EDITOR='/path/to/Unity.app/Contents/MacOS/Unity' bash tools/test-unity.sh
```
In the Editor, run `FORMA > Build Quest Development APK`. It includes **only** StudioQuest, sets development identifier `com.forma.studio.dev`, IL2CPP and ARM64, and writes `Unity/Builds/FORMA-Studio.apk`. It deliberately does not sign a release build or configure proprietary package credentials. Ensure XR setup and manifest validation have already passed. Use Build and Run from Unity or:
```sh
adb install -r Unity/Builds/FORMA-Studio.apk
adb shell monkey -p com.forma.studio.dev 1
adb logcat -s Unity
```
Never record a successful APK build unless the Editor returns success and the output exists. Passing EditMode tests is not a successful Android build. No APK has been produced on the authoring host.

## CI
The included workflow compiles/runs only the linked pure C# domain harness on .NET 8 and checks repository invariants. It needs no Unity license or Meta authentication. Unity EditMode/Android CI must be enabled only after committing the real generated project and resolved dependency lock, provisioning a legitimate Unity license, and confirming whether Meta packages may be restored in that CI environment under their terms. Use `tools/test-unity.sh` on that runner and upload the XML/log artifacts; device acceptance remains a separate manual gate. No fake green Unity-build CI job is supplied.

## Phase 2 A/B scene integration
Newly generated Preview/Quest scenes also compose OnboardingController, SetupPanel and the three bundled coach presets. Existing scenes are never overwritten: retain the old scene for Phase 1 comparison, rename/archive it explicitly, then generate a new scene through the actual Editor. Keep the Built-in render pipeline; no new package is needed by the setup UI.

Run EditMode tests (including UnityProfileCodec) and then the new PlayMode spatial-panel smoke test:
```sh
UNITY_EDITOR='/path/to/Unity.app/Contents/MacOS/Unity' bash tools/test-unity.sh
UNITY_EDITOR='/path/to/Unity.app/Contents/MacOS/Unity' UNITY_TEST_PLATFORM=PlayMode bash tools/test-unity.sh
```
The PlayMode test uses actual Unity rendering/physics components, not headset simulation; it still does not verify Quest compositor behavior or comfort. Validate both generated scenes manually. Test first launch, all optional fields skipped, Turkish glyphs, three looks/styles, seated restrictions, no-compatible-workout behavior, mute/volume, reduced idle motion, manual placement, failed saves, reset, profile-only export, complete local deletion, returning users and focus loss on every setup step. Physical-space confirmation must not survive interruption or authorize a future workout.

Local profile export path: `Application.persistentDataPath/forma-exports/setup-profile.json`. On development devices use the actual Android sandbox path exposed by the development build and authorized ADB access; no consumer share picker is claimed. Source-side requested SDK pins remain unchanged and unresolved; do not infer an installed package from this update.
