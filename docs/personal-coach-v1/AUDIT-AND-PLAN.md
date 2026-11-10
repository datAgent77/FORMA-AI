# Personal Coach V1 — technical audit and implementation plan

Audit before implementation, 2026-11-10.

## Existing working architecture

- `tools/WebSimulation/Program.cs`: loopback ASP.NET Core .NET 8 host, same-origin POST protection, browser cookie isolated disk directories, static Three.js modules. No cloud or LLM dependency.
- `Unity/Assets/Forma/Domain/Workout.cs`: rendering-independent deterministic interval machine, explicit safety start/resume, pause/rest/completion. Linked into .NET, not a Unity runtime requirement.
- `tools/WebSimulation/Simulation.cs`, `DailySimulation.cs`: profile setup, exercise constraints, session lifecycle and JSON history. Existing records distinguish completed, aborted and interrupted sessions.
- `Product/ProductProfile.cs` / `Storage/ProductProfileRepository.cs`: schema-2 setup preferences and atomic profile writes. Not a complete athlete profile; profile updates/history are separate.
- `Product/DailyExperience.cs`: deterministic preference/frequency-based starter recommendations. No difficulty-driven progression or persistent recommendation journal.
- `wwwroot/xr.mjs`, `holographic-studio.mjs`, `studio-composer.mjs`: real geometric environment and rigged holographic presentation. Temporary procedural march/shoulder motion; not validated exercise capture. XR AR request/ray input exists; no physical Quest verification.
- Current storage defaults to a temporary directory and browser identifier expires after seven days. These are development-tool limitations that must be corrected for athlete persistence, without deleting legacy files.

## Missing product behavior

Athlete identity, full profile fields and measurements provenance; explicit persistence migrations; full warm-up/main/cooldown library and exercise feedback; persisted coach memory and feedback-driven recommendations; daily/weekly/monthly analytics; complete demonstration coverage; second environment; consistent localization. No LLM exists or is required.

## Plan and verification gates

A. Add validated reusable athlete types and a versioned local repository to the existing application. Integrate profile create/update/export/delete into the existing studio. Preserve legacy setup/history, block overwriting invalid/future data, use atomic saves, test reload and host restart.

B. Reuse the interval machine behind a personal session service with warm-up, exercise/rest, cooldown, feedback gates and terminal outcomes. Store only explicit manual repetitions and distinguish timer completion from athlete-reported performance.

C. Read actual persisted session/feedback history through a coach-memory service; generate bounded, explainable deterministic recommendations for beginner, consistent, returning, high-difficulty and discomfort cases. Persist decisions. Test A–C before D–F.

D–F. Build actual-data progress/measurement views, extend coach presentation with reliable animations or explicit instructional fallbacks, and add one geometric outdoor environment. Keep these statuses separate from A–C. No native/Unity build work.

All code stays in the existing repository/application. New domain code must remain independent of HTTP/Three.js. Existing C# files under the historical Unity directory are shared source, not a Unity migration. Fixtures must remain isolated and must never populate a user's history. Desktop, synthetic/API and physical headset verification are reported separately.
