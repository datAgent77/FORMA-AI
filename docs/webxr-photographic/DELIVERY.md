# Photographic video studio correction — 2026-11-04

The user rejected the earlier low-poly reference demo and chose **a coach that looks like real human video, prioritizing visual resemblance**. The displayed demo now uses actual recorded human footage rather than the Quaternius character. Native Unity remains a separate, incomplete product milestone.

## Delivered

- `tools/WebSimulation/wwwroot/video-coach.mjs`: a recorded full-body person, alpha-composited as a 1.75 m video plane. A single H.264 frame packs RGB on the left and the matching alpha on the right, so color/matte cannot drift. A second instance renders an explicitly illustrative silhouette in the timer panel. This is a frontal 2D video in 3D space, **not a volumetric avatar**.
- `coach-media-policy.mjs`: presentation playback stops for pause, rest/completion, visibility loss, offline state, reduced motion and unsupported exercise demonstration. It never drives workout time, approves space or resumes a paused workout. Existing C# workout state and monotonic ticks are retained.
- `assets/studio/room-photo.png`: AI-generated photographic room plate with timber/glass/rug details. It is a desktop example room, not a live camera feed. Desktop orbit dragging is removed because the plate is a photograph. The actual `immersive-ar` path still uses transparent compositor rendering; the CSS background is not drawn into the XR layer.
- `assets/studio/dumbbell-glove.png`: AI-generated transparent simulated foreground prop, styled to the reference. It is explicitly described as simulated and is hidden during actual running/paused exercise and in XR. No equipment or hand tracking has been added.
- `xr.html`, `xr.css`, `xr.mjs`: sparse scene composition, coach position/framing aligned to the reference, right-hand translucent yellow timer, lower exercise strip, circular pause control and a compact control drawer. Default onboarding/safety confirmation remains explicit. No default exercise is started by loading the visual preview.
- Original low-poly/GLB resources are retained for history/development compatibility, but the active page no longer imports the low-poly scene/model module.

## Footage and limits

Source: [Woman Warming Up — Polina Tankilevitch, Pexels 6525515](https://www.pexels.com/video/woman-warming-up-6525515/), under the [Pexels License](https://www.pexels.com/license/). No endorsement or professional qualification is implied. The original footage is not redistributed; the app includes a modified cropped/matted excerpt. Source and output hashes/processing are recorded in `assets/studio/PROVENANCE.json` and dependency notices.

Only original frames 17–25 are used, excluding the source's high-knee sequence. They are slowed and played forward/backward into a **1.2-second small-step preview loop**. This is an actual video, but not a newly filmed complete workout. It remains a limited prototype demonstration. Shoulder sessions hold the video still and retain written guidance; a shoulder-roll video has not been fabricated. A full, reviewed clip set matching each prescribed movement is still required for production.

Offline matting used RVM v1.0.0 (GPL-3.0) via ONNX Runtime on this public stock clip. Neither RVM/model nor Python/runtime packages are shipped, and no user camera/motion footage was captured or uploaded. Room and prop images were generated with the built-in image tool and copied into the project.

The large ring remains **Hazır** before a workout, then uses real shared C# interval values. It does not display an invented 19-second workout for visual imitation. Diagram shading is illustrative, never measured activation or body/form tracking. No weights exercise is added.

## Verification

- **46 web/API/AR-guard tests passed**, including local video/range serving, RGB/alpha metadata and media pause/visibility/offline/reduced-motion gates. Existing intro/work/pause/rest timing checks retained.
- Browser: real video loaded with `readyState=4`, dimensions `324×536`, actual preview playback observed. Clicking **Duraklat** changed `video.paused` to `true`; explicit **Videoyu oynat** changed it back to `false`. No safety boxes or physical-space claims were selected during this browser test.
- Scene reviewed in the reference's `1322×892` landscape viewport and the default narrow viewport. Temporary viewport override was reset. `preview-wide.png` is an actual browser screenshot, not a generated app mockup.
- JavaScript syntax and repository source invariants passed. No Unity build/APK or Quest hardware test was performed.

## Open

[Live local demo](http://127.0.0.1:5090/xr.html). **Seans kontrolleri** opens play/setup/session/Quest entry controls. **Kurulum** opens onboarding. Circle pause and Space stop motion. Run with `FORMA_SIM_PORT=5090 bash tools/run-web-simulation.sh`; use `FORMA_DOTNET` if needed. On a supported USB-connected Quest use `FORMA_SIM_PORT=5090 bash tools/open-quest-webxr.sh`. Hardware passthrough, video comfort/frame rate, controller/hand selection and codec behavior still require physical validation.
