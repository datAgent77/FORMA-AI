# Native A continuation — Editor installation detected, but unusable

Date: 2026-11-04. **Overall: BLOCKED.** This replaces the earlier current-host statement “Editor not installed”; that statement remains valid only for the initial audit snapshot. No Unity project, scene or APK has been generated.

## Actual findings and attempts

- **PARTIAL INSTALLATION:** Unity Hub and an executable at `/Applications/Unity/Hub/Editor/6000.6.8f1/Unity.app/Contents/MacOS/Unity` now exist. The executable is ARM64. The folder version is 6000.6.8f1, not the previous requested 6000.3.27f2. [Official 6000.6.8f1 release](https://unity.com/releases/editor/whats-new/6000.6.8f1). No successful invocation establishes this as the project's actual Editor version.
- **FAIL:** Real `-batchmode -quit -createProject` invocation through `tools/bootstrap-unity.sh` ended with **exit 137 / Killed: 9**, before creating an Editor log or `Unity/ProjectSettings/ProjectVersion.txt`. [Console output](evidence/editor-6000.6.8/bootstrap-console.txt). No Unity build was attempted by that command.
- **FAIL:** `Unity.app/Contents/Info.plist` is missing. `codesign --verify --deep --strict` returned exit 1: `invalid Info.plist (plist or signature have been modified)`. [Signature output](evidence/editor-6000.6.8/codesign-check.txt). This demonstrates an incomplete/invalid app bundle; the exact OS kill reason was not independently traced, and no licensing failure is inferred.
- **MISSING:** the selected Editor has no AndroidPlayer/Android Build Support module. Only MacStandaloneSupport was found. The host's independent Android SDK cannot supply Unity's missing Android player/Editor extension.
- **LOW FREE SPACE:** filesystem inventory showed approximately 1.0–1.5 GiB free during these checks. That is a concrete installation/build constraint; the logs examined did not prove that disk space caused the missing plist.
- **Hub queue:** the sanitized queued records listed **Web Build Support** and **Documentation**, not Android. [Queue summary](evidence/editor-6000.6.8/hub-queued-items.json). This is a timestamped observation, not a claim about future installer progress. No account data or signed URLs were retained.
- **BLOCKED:** selecting the installed Hub via Computer Use returned **“Computer Use permissions are not granted.”** Hub could not be operated to inspect/finish its installation UI. No security setting, signature, quarantine or licence agreement was altered.
- **NOT TESTED:** licence activation, SDK import/API compatibility, Unity compilation, native rendering/input, Android build and Quest hardware. A subsequent `adb devices` query again listed no authorized device.

## Tooling fixes and executed checks

- `tools/native-preflight.py` now distinguishes finding an executable from a structurally complete macOS app. Missing Info.plist and Android module are explicit blockers. Inventory records the app path, module path, free bytes and that launch verification has not occurred.
- `tools/bootstrap-unity.sh` now refuses an incomplete macOS app before executing it, then requires its existing code signature to verify. It does not repair/re-sign an app or bypass OS checks. With this installation, the updated guard returned **exit 2**, identifying the missing plist. [Output](evidence/editor-6000.6.8/bootstrap-guard-console.txt).
- **PASS:** Python tooling suite **5 tests**, including a deliberately incomplete temporary app fixture and proof that bootstrap never invokes it or creates a project. [Output](evidence/editor-6000.6.8/preflight-tests.txt).
- **PASS:** shell syntax, source invariants and diff checks. No C# runtime source changed in this continuation, so earlier .NET results were not rerun or counted as new Unity evidence.
- **BLOCKED:** native foundation build wrapper exited **2** before BuildPipeline/Editor invocation. [Updated inventory](evidence/editor-6000.6.8/inventory.json), [build preflight](evidence/editor-6000.6.8/build-preflight.txt).

## Concrete next steps

1. Free enough space for the installer/module sizes displayed by Hub plus project/Library/build working space. No personal files or existing Unity installations were deleted by this work.
2. In the official Unity Hub, finish or repair the selected Editor installation. Verify `Contents/Info.plist` exists, the app signature passes and Editor opens normally. Do not synthesize plist files or ad-hoc re-sign the binary to make the check pass.
3. Add **Android Build Support**, **Android SDK & NDK Tools**, and **OpenJDK** for that same Editor via Hub. [Official installation instructions](https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/android/getting-started/sdksetup/install-dependencies).
4. For 6000.6.8f1+, Unity lists bundled SDK Build Tools 36.0.0, command-line tools 21.0, platform tools 37.0.1; Unity 6 uses NDK r27c **27.2.12479018** and OpenJDK **17**. The host's NDK 27.1.12297006 is not the listed NDK. Use the matching Hub modules; [official supported dependency table](https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/android/getting-started/sdksetup/supported-dependency-versions). SDK tool versions describe the bundled baseline, not a claim that every other SDK version is unusable.
5. If Hub operation should be delegated to this agent, enable Computer Use permission. Licence/login/OS permission steps may still require direct user action; no licence has been accepted automatically.
6. Once Editor is healthy, set `UNITY_EDITOR` to its verified executable and follow [QUEST-FOUNDATION.md](QUEST-FOUNDATION.md). Reconcile the newer Editor with actual resolved SDKs before updating project pins. Then create/import the real project, run Unity tests and the Foundation APK build. **Milestone B remains unstarted.**
