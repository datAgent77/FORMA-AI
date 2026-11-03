# Native Quest installation and launch

**Not yet runnable from this checkout.** No Unity import/build or headset test has occurred. These steps turn authored source into an actual Editor project; record and resolve import/API errors rather than treating this guide as proof of compatibility. Run from the repository root.

## Reviewed dependency baseline (2026-11-02)

- Unity **6000.3.27f2**, 3D Built-in Render Pipeline, Android Build Support **including that Editor's SDK/NDK and OpenJDK**. Install via Hub and activate a license appropriate to your organization. [Official release](https://unity.com/releases/editor/whats-new/6000.3.27f2).
- Meta XR Core **207.0.1**, official scoped registry `https://npm.developer.oculus.com`, scope `com.meta.xr`. Registry metadata declares Unity `6000.0` and XR Hands `1.7.2`. [Registry metadata](https://npm.developer.oculus.com/com.meta.xr.sdk.core), [Core download/terms](https://developers.meta.com/vr/downloads/package/meta-xr-core-sdk/).
- OpenXR **1.17.0**. Official documentation lists Unity 2022.3+ and Quest ARM64/Vulkan support. This does not establish that FORMA's Meta/OpenXR combination works. [Official manual](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.17/manual/index.html), [registry](https://packages.unity.com/com.unity.xr.openxr).
- Test Framework **1.4.6** is the existing requested test pin, not a newly validated runtime dependency. Resolve in Editor; if unavailable/incompatible, review official Package Manager metadata before changing it.

This is a **candidate baseline**, not an installed or device-tested matrix. Commit actual `ProjectSettings`, package manifest, lock, scene, prefab and `.meta` files only after the Editor produces them. Do not hand-author those generated files. Neither the Meta Movement SDK nor a third-party avatar is required for this slice.

Meta SDK is proprietary, not covered by this repository's license. Review Meta SDK/license terms and third-party notices before downloading/importing; do not redistribute SDK source. Unity Editor/packages have their own terms. The manifest helper requires an explicit local license-review acknowledgement; it does not accept any license for you. No paid/AI/voice service is involved.

## Create and import

1. Install the Editor and Android module above in Unity Hub. Set the actual executable, for example:

   ```bash
   export UNITY_EDITOR='/Applications/Unity/Hub/Editor/6000.3.27f2/Unity.app/Contents/MacOS/Unity'
   bash tools/bootstrap-unity.sh
   ```

   Bootstrap calls the real Editor `-createProject`; it never synthesizes metadata. If the installed Editor cannot create in the existing source directory, use Hub to create a **3D Built-in** project in a temporary directory, move its Editor-generated project configuration into `Unity/` while preserving `Unity/Assets/Forma`, then reopen/import. Keep an untouched backup. Do not choose URP: current materials require `Unlit/Color`.

2. Close Editor after project creation. Review the dependencies and licenses, then merge the candidate manifest:

   ```bash
   FORMA_META_LICENSE_REVIEWED=1 python3 tools/prepare-native-manifest.py --include-meta
   ```

   It refuses to operate without real project metadata, preserves unrelated template dependencies, and rejects conflicting versions for review. `config/unity-manifest.native.json` is a recipe, **not** proof of installed packages. Open Editor, allow resolution/import, review Console and resolved `Packages/packages-lock.json`. Resolve any API/compiler errors before continuing.

3. Switch build target to Android. Configure XR Plug-in Management: initialize XR on startup, Android **OpenXR** loader, Meta Quest support feature and Touch Controller interaction profile. Configure hand tracking as required by Core's current OpenXR guidance; run **OpenXR Project Validation** and **Meta Project Setup Tool**, review/fix errors. Do not enable two competing Android loaders. Use Built-in rendering, Vulkan only, linear color, multiview/stereo settings recommended by Meta. Set minimum API 32 or the higher requirement reported by the installed SDK, target the installed/current required Android API. Use the Editor's embedded SDK/NDK/JDK rather than assuming the host NDK is compatible.

4. Add `FORMA_META_XR` to Android scripting define symbols. Wait for compilation. The conditional `Forma.Meta` assembly must resolve `Oculus.VR`; confirm the installed SDK assembly layout and fix any version/API differences explicitly. This integration source has **not compiled against Core 207.0.1** on the authoring host.

5. Run **FORMA > Create Quest Studio Scene**. It instantiates the SDK's actual `OVRCameraRig`, attaches `OVRPassthroughLayer` underlay and native adapter, and generates Unity meshes/TextMesh/collider controls and temporary primitive coach. This operation must produce `Assets/Forma/Scenes/StudioQuest.unity` in the Editor. Save all generated assets. It refuses to overwrite an existing scene.

6. In the rig's OVRManager set floor-level tracking, passthrough enabled/supported, hand tracking support **Controllers and Hands** and the current SDK hand-tracking options required by Project Setup. Verify the resulting Android manifest declares passthrough and hand-tracking features using SDK setup, not raw camera access. No raw camera permission is needed for compositor passthrough. Keep clear alpha 0 on eye cameras and underlay opacity 1; do not add a skybox, opaque floor or fullscreen fade.

7. Configure actual SDK **OVRHandPrefab** instances under left/right hand anchors. Set each prefab's OVRHand, OVRSkeleton and OVRMesh handedness correctly; set pointer pose root to the rig TrackingSpace as described by the installed SDK. Use the SDK's visible hand meshes/renderers. Assign the left/right `OVRHand` components to the generated **FORMA Studio > MetaHandPointer** fields. This is an explicit manual step, not yet automatic scene generation. [Meta hand setup](https://developers.meta.com/vr/documentation/unity/unity-handtracking-hands-setup/), [OVRHand API reference](https://developers.meta.com/vr/reference/unity/v71/class_o_v_r_hand/) (reference is older than the candidate Core release; verify current imported API).

8. Verify StudioPointer tracking-space reference, CoachAppearance floor-origin reference, all platform/UI/controller references, metre scale `(1,1,1)`, and a coach height around 1.85 m (preset ornament may extend above head). `Move coach` places it 2.6 m away using the calibrated floor origin, independent of eye height. Confirm there are no physical obstacles in that location manually. No room-mesh/obstacle detection or persistent spatial anchor is implemented. Regenerate/review placement after recenter; validate seated/standing readability and reach.

## Tests and actual APK build

Run existing `tools/test-unity.sh` with the installed Editor for EditMode/PlayMode tests; inspect result XML and logs (the authoring host has not run these). Then close Editor and run:

```bash
bash tools/build-native-quest.sh
```

Read-only Python preflight rejects missing metadata/packages/scene. The Editor build path also checks Android target, OpenXR loader, Vulkan, minimum API, rig/layer, hand references, floor origin and timer/pause/exit references. This is **not** a replacement for Project Validation or hardware acceptance. `Unity/TestResults/native-build.json` records invocation/result, exact registered packages, errors and APK SHA-256. An APK exists only after a real successful BuildPipeline run. Default output: `Unity/Builds/FORMA-Studio.apk`, development-only application id `com.forma.studio.dev`, IL2CPP ARM64. Do not use development signing for a store release.

## Install on Quest 3 / 3S

Enable Developer Mode for the account/device through Meta's current developer setup, connect USB, allow USB debugging in the headset, establish a safe boundary and clear equipment/people/pets. Use the Android tools installed by Unity:

```bash
adb devices
adb install -r Unity/Builds/FORMA-Studio.apk
adb shell monkey -p com.forma.studio.dev -c android.intent.category.LAUNCHER 1
adb logcat -s Unity AndroidRuntime
```

Alternatively open FORMA Studio from Unknown Sources. If the package does not install, inspect the actual APK manifest/API/ABI and adb error. Never report installation from the existence of an APK alone.

Complete onboarding, choose the starter, confirm visible clear space, start, follow written instructions and the temporary coach; pause with B/Y or the visible pause control. Hand-only mode uses a validated hand ray + index pinch to select the visible pause/exit controls; do not override the system menu gesture. Lost valid pointer pauses an active session; reacquisition never auto-resumes and requires release before selecting. Confirm space again before Resume. End/complete, review history, exit and relaunch to verify persistence. Exit is pause-first, then explicit confirmation; passthrough must remain visible until the OS takes over.

## Physical acceptance gate — all unchecked

Record date, app commit/APK hash, resolved SDK versions, headset model and OS for **both Quest 3 and Quest 3S**. This host has tested none of these:

- [ ] First launch permissions, visible full-colour passthrough, no black/opaque frame during setup/work/pause/summary/exit.
- [ ] Correct floor calibration, life-size temporary 3D coach, manually clear 2.6 m placement, seated/standing legibility and recenter behavior.
- [ ] Left/right Touch rays + trigger, B/Y immediate pause, controller disconnect/reconnect and hand/controller switching without accidental selection.
- [ ] Left/right hand meshes, pointer/pinch; confidence loss, occlusion, lighting changes and system gesture stop selection; tracking loss pauses; explicit clear-space confirmation and Resume required.
- [ ] Visible timer, intro, both exercises, work/rest, pause without time advancing, completion cue and summary; no exaggerated form/health claims.
- [ ] Local history/profile survive quit/relaunch and interruption; failed/full storage blocks new sessions; corrupt files handled; delete/export operate locally.
- [ ] Headset removal, loss of focus, Quest menu, tracking/passthrough initialization failure all block/stop exercise without automatic restart.
- [ ] Safe exit reachable seated/standing/hand-only, OS exit works, no immersive transition; boundary remains available.
- [ ] Sustained headset frame rate/thermal comfort; native memory allocations, rendering, hand accuracy and real-time timing profiled.

Remaining work: actual Editor import/API reconciliation, resolved SDK integration and manifest review, hand prefab wiring, generated scene/prefab commit, Unity tests, successful Android build, both-device acceptance and any fixes those reveal. Native milestone stays incomplete until those pass.
