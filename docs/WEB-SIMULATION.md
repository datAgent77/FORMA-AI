# FORMA web simulation

A local browser test surface for the existing product. It compiles the actual `Forma.Domain`, `Forma.Product` and `Forma.Storage` C# files into a development-only .NET 8 host. JavaScript renders server snapshots and forwards actions; it does not reimplement workout timing or onboarding transitions. No Unity project, Meta SDK, external website, AI backend or paid service is needed.

## Start
Requires .NET 8 SDK, plus Node.js 22 and curl for automated HTTP tests. From repository root:
```sh
bash tools/run-web-simulation.sh
```
Open [the simulator](http://127.0.0.1:5078/). Stop the host with Ctrl+C. If dotnet is not on PATH, pass the installed executable:
```sh
FORMA_DOTNET='/absolute/path/to/dotnet' bash tools/run-web-simulation.sh
```
Set `FORMA_SIM_PORT` to select a different loopback port. The host intentionally binds IPv4 loopback; do not publish or expose it as a production service. Scripts set `DOTNET_USE_POLLING_FILE_WATCHER=1` for this process because native macOS sandbox notifications stalled the initial host configuration in this environment. No system-wide settings are changed.

## What can be tested
- English/Turkish setup, independent coach appearance/personality, optional profile fields, seated filtering and incompatible movement blocking.
- Required setup safety checks; a new physical-space check before each workout.
- Exercise selection, introduction, work/rest/set timing, pause/resume/end and completion summary.
- Test-lab focus/tracking/passthrough loss, explicit recovery, frozen paused time and manual elapsed-time jumps.
- Profile write failure/retry, actual codec-injected C# profile persistence, browser/profile reload, per-browser isolation, profile/session export and synthetic-data reset.
- Two-action exit and no automatic workout start or resume.

The browser uses mouse/touch; Space pauses when focus is outside form controls. The illustration is an original SVG development coach, **not Unity rendering**. Optional speech and physical camera/tracking are absent. Test controls synthesize readiness signals and elapsed time; recorded values are test data, not measurements of a person's workout.

## Test runner
```sh
bash tools/test-web-simulation.sh
```
This builds the host, creates a separate temporary data directory and server on port 5079, waits for health readiness, checks JavaScript syntax, runs Node's built-in HTTP test runner, then stops its server and removes its own temporary data. It refuses an already occupied test port so it cannot accidentally reset an unrelated development session. Use a different `FORMA_SIM_PORT` if needed.

The workflow `.github/workflows/domain.yml` runs the original domain/product harnesses and the web test runner. The HTTP tests exercise real HTTP requests into compiled C#; they do **not** simulate a browser DOM. Actual browser checks were performed separately using the in-app browser and are recorded below.

## Storage and scope
The test host keeps synthetic per-browser state in memory and files in the OS temp directory `forma-web-simulation/<random-browser-id>`, or under `FORMA_SIM_DATA` when explicitly set. A SameSite/HttpOnly cookie selects the browser's isolated test profile. Tests generate their own sessions. Never enter real health or identifying information. No telemetry, remote fonts, camera access, voice capture or third-party requests are used by this tool.

`ProductProfileRepository` is the same file repository used by Unity through another codec. This host uses System.Text.Json; it does not prove Unity JsonUtility serialization. Simulation history is a separate adapter using SessionRecord, not Unity LocalStore. Product profile and simulated history survive local host restart; an active WorkoutMachine is not reconstructed. Unfinished records remain Interrupted. This does not implement Milestone C recovery.

A browser reload normally reconnects to the same in-memory simulation; Reload saved profile deliberately reconstructs the setup from disk. Reset affects only this browser's test directory and never accesses Quest files. Export is explicitly marked `simulationOnly` and includes only this simulated profile/history.

## Actual results — 2026-11-02
- .NET 8 Release build: succeeded with zero warnings and zero errors.
- **22/22 HTTP scenarios passed**, zero skipped. [Recorded TAP output](phase2/evidence/web-api.txt).
- Browser execution: switched setup to Turkish, selected Mira + Calm Guide, skipped optional fitness fields, verified Continue disabled without both safety checks, saved and entered Studio.
- Browser execution: confirmed clear space, started a workout, switched simulated tracking off, observed Paused and disabled Resume, restored tracking, explicitly reconfirmed space and resumed, then advanced the test clock to completion. Recorded result: 80 simulated work seconds / four timed sets.
- Browser responsive checks: 1440 px viewport reported 1425 px document width (scrollbar excluded), and 375 px viewport reported 360 px document width; no horizontal document overflow at either tested size. Temporary viewport overrides were reset.
- [Desktop capture](phase2/evidence/web-desktop.jpg) and [mobile capture](phase2/evidence/web-mobile.jpg) are screenshots of the functioning browser simulator, not headset captures.
- Initial HTTP attempt could not reach a ready host. After identifying the native file-watcher startup stall and enabling process-local polling, the final isolated test runner passed all scenarios.

The browser checks led to queuing action requests so rapid field changes are not silently dropped. They also exposed a missing per-session clear-space reset; the simulation and Unity integration now clear it on the first terminal transition while preserving a later fresh confirmation. The shared workout domain was unchanged. The web regression verifies the simulation orchestration; Unity's corresponding code change still needs Editor/device verification.

## What web tests cannot establish
Unity/Meta API compilation, generated scene validity, camera alpha/compositor behavior, actual passthrough visibility/latency, headset/controller mapping, hands, room boundaries/obstacles, floor/seated alignment, frame time, thermals, headset comfort, release signing, store entitlement checks or store approval. The Unity scene, JsonUtility and hardware acceptance gates remain open. Passing web tests is not grounds to claim that A/B headset acceptance or Milestone G is complete.
