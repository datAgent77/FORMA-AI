# FORMA WebXR development demo — 2026-11-02

Run `bash tools/run-web-simulation.sh` and open [WebXR Studio](http://127.0.0.1:5078/xr.html). A local pinned Three.js 0.187.0 renderer draws an original procedural 3D coach, live canvas-text panels and controller-ray buttons. State/timers/actions use the existing C# HTTP host; this is a browser adapter, not a Unity build or an AI backend. Complete profile setup on the main page first, using the same browser.

## Quest launch
The host deliberately remains loopback-only. With a developer-enabled Quest connected over USB and USB debugging authorized:
```sh
# Terminal 1: keep the local host running.
bash tools/run-web-simulation.sh
# Terminal 2: forward only this local development port and open Quest Browser.
FORMA_ADB='/absolute/path/to/adb' bash tools/open-quest-webxr.sh
```
For multiple devices set `FORMA_QUEST_SERIAL` explicitly. The helper verifies that the selected Android model identifies as Quest, uses `adb reverse tcp:5078 tcp:5078`, and opens `http://127.0.0.1:5078/xr.html`. On the headset, confirm the cleared, equipment-free space, then select the passthrough button and handle the browser's XR permission prompt yourself. No headset session is entered automatically. A normal HTTP LAN IP is not a substitute for the WebXR secure-context requirement; do not disable browser security checks. Use localhost forwarding or a properly trusted HTTPS deployment.

## Actual WebXR behavior
- Uses feature detection and an explicit user gesture to request `immersive-ar` with required `local-floor`.
- Never falls back to `immersive-vr`. An opaque blend mode is rejected and ended.
- During AR, the preview floor/grid disappear and rendering has a transparent background. No room-sized opaque floor/skybox hides passthrough.
- Controller `select` rays operate large 3D buttons. B/Y secondary button index is a convenience where xr-standard mapping supplies it; verify actual Quest mapping. The large Pause/Exit controls remain the documented fallback.
- Tracking loss, focus/visibility loss, connection errors and session end pause activity; no automatic resume. Opening the viewer pauses an already-running session. Each resume needs a fresh clear-space check through the model.
- Space placement is manual, without physical obstacle detection. No raw camera, microphone, hand-joint or body-tracking permission is requested. Poses are used to render locally, not uploaded as motion telemetry.
- The 2D page provides the same accessible controls and a DOM session-state readout. Mouse/touch 3D interaction is a desktop preview, not proof of headset tracking.

An initialized AR blend mode is not a measurement of actual compositor image visibility or room safety. Stop for loss of real-world visibility and use system controls. No weights/equipment or immersive transition is part of this demo.

## Executed verification
- Current in-app desktop browser rendered the actual WebGL scene, loaded the shared C# state and displayed the real runtime result: `immersive-ar` unsupported; entry button remained disabled.
- Clicked clear-space confirmation, recommendation, start, then pause in the actual browser. DOM session state changed Introduction → Paused, 10 seconds remaining. No console errors/warnings were observed.
- **42/42 HTTP/guard scenarios passed**, including six WebXR asset/capability/secure-context/AR-request/blend-mode tests. XR API objects in guard unit tests are explicitly mocks, not a fabricated device session. [TAP evidence](phase4/evidence/web-tests.txt).
- `adb devices` successfully ran and showed **no connected devices**. Real Quest AR session, passthrough visibility, input mapping, floor alignment, comfort and performance remain **not tested**.
- No browser extension/emulated XR runtime was installed; no unsupported browser was made to claim real headset support.

## Dependencies and sources
Three.js was verified through official NPM package metadata before fetching version 0.187.0. Only its two bundled renderer modules and MIT license are vendored, with hashes in `wwwroot/vendor/three/provenance.json`. No runtime CDN calls occur.

Official references: [Three.js WebXRManager](https://threejs.org/docs/pages/WebXRManager.html), [WebXR Device API](https://www.w3.org/TR/webxr/), [Meta browser mixed reality](https://developers.meta.com/vr/documentation/web/webxr-mixed-reality/), [Meta remote debugging](https://developers.meta.com/vr/documentation/web/browser-remote-debugging/). Direct Meta pages yielded limited readable content here; official indexed excerpts describe immersive-ar passthrough and adb reverse. Verify full live Quest Browser behavior on the target headset before release.
