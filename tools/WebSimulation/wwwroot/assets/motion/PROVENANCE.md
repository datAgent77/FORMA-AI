# Standing alternating knee raise — motion provenance

- Exercise ID: `standing_alternating_knee_raise`; existing workout/history alias: `knees`.
- Source: `standing-alternating-knee-raise.source.json`, original manually authored full-body pose landmarks, contact phases and cues for this project.
- Bake: `tools/motion/bake-knee-raise.mjs`, original offline analytical two-bone constraint/retargeting tool. Output is `standing-alternating-knee-raise.clip.json`, 60 Hz skeletal position/quaternion tracks. Runtime uses AnimationMixer; no primary sine-wave knee motion.
- Motion rights: **LicenseRef-FORMA-Project-Owned**. Newly authored motion source and clip are project-owned; no external animation asset license is introduced. Redistribution follows the repository owner's terms. This notice does not relicense the repository or dedicate new project IP to the public domain.
- Character remains the existing Quaternius Woman Casual model, **CC0-1.0**, source/hash in `../coach/PROVENANCE.json`. [CC0](https://creativecommons.org/publicdomain/zero/1.0/). No replacement mesh, exercise mocap pack or downloaded animation was introduced.
- Three.js remains vendored MIT, r187.
- Not motion capture, not clinically/biomechanically validated, and not a full human balance simulation. Numerical contact/loop tests and multi-angle visual review have limited scope; do not describe this asset as validated professional motion capture.

Rebuild from repository root: `node tools/motion/bake-knee-raise.mjs`. The tool verifies the exact source GLB SHA-256. Changed rig/rest pose requires retargeting review. A changed or missing asset fails explicitly; the legacy sine-driven knee placeholder is not used as a fallback.
