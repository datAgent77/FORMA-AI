# Scene organization
The repository contains source, not fabricated Unity YAML. The Editor builder creates:
- `StudioPreview.unity`: desktop interaction and coaching preview, clearly labeled; not passthrough.
- `StudioQuest.unity`: actual Meta camera rig, passthrough underlay, coach, safety controls, workout panel and local history panel.

Use one scene per build. All state is composed by `StudioController`. Domain logic has no scene dependencies. Future Worlds and Mirror scenes are intentionally absent. Commit the real Editor-generated scenes, material assets, settings and .meta files after import. Never regenerate over a customized scene without explicitly renaming the old one.
