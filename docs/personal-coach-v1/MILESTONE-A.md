# A — persistent athlete profile

Implemented in the existing holographic studio, 2026-11-10. This is not a claim that Personal Coach V1 or milestones B–F are complete.

## Implementation

- `Unity/Assets/Forma/Product/AthleteProfile.cs`: reusable typed profile/input, seven goal categories, optional age range/height/weight, equipment inventory, duration/environment preferences, weekly training days, structured voluntary exclusions, optional note, units, language, server-owned identity and timestamps. Canonical measurements are explicitly manual kg/cm, not headset readings.
- `tools/WebSimulation/AthleteRepository.cs`: schema 2, schema-1 migration, atomic durable save, validation, revision checks against stale tabs, preservation of invalid/future documents.
- `LocalDataLocation.cs`: default storage moved from temporary files to git-ignored `tools/WebSimulation/.local-data`, outside static `wwwroot`. First startup copies existing known files without overwriting newer files. A marker prevents deleted data from being resurrected on later starts. Explicit `FORMA_SIM_DATA` remains supported and bypasses legacy import.
- `AthleteSimulation.cs`, `Simulation.cs`, `Program.cs`: same-origin profile API, existing full-data export extended with raw athlete/legacy documents, confirmed identity/revision-bound deletion, existing reset extended to athlete data, legacy copies removed during deletion, sliding one-year browser cookie. No new cloud/backend service.
- `wwwroot/athlete.mjs`, `athlete-copy.mjs`, `athlete-units.mjs`, `xr.html`, `xr.css`: profile form inside the current studio, edit/save/status/error handling, optional measurements, metric/imperial conversion, Turkish/English profile copy, export and explicit typed deletion confirmation. Existing hologram/session UI preserved.
- Stored movement exclusions fail closed for conflicting existing workouts. Profile changes/deletion are blocked while a workout is active or paused.

## Verification

- Release .NET build succeeded with zero warnings/errors.
- 58 web/API tests passed, including five athlete tests: create/reload/edit/export/delete, history preservation, invalid input/stale revisions/write failure, schema migration/invalid/future data preservation, units and movement exclusions.
- `tools/test-athlete-restart.mjs` passed using isolated child processes and temporary fixture data. It verifies actual host restart persistence, goal edits, default-directory migration, removal of legacy copies and no resurrection after deletion/restart.
- Source invariants, JavaScript syntax and git whitespace checks passed.
- Desktop Chrome: created a clearly named test profile, selected Turkish and a goal, saved, reloaded, edited the goal and saved again. Profile identity/creation date stayed constant; update time changed. No real athlete measurements or fabricated completions were entered.

Evidence: [API tests](evidence/milestone-a-tests.txt), [restart/migration test](evidence/milestone-a-restart.txt).

## Reproduce

```sh
FORMA_SIM_PORT=5095 bash tools/run-web-simulation.sh
bash tools/test-web-simulation.sh
node tools/test-athlete-restart.mjs
```

Requires .NET 8; set `FORMA_DOTNET` to its executable if it is not on PATH. Open http://127.0.0.1:5095/xr.html → Profil. Create a profile, save, reload, reopen and change the goal. Export downloads all current data through `/api/export`. Deletion requires typing SİL (Turkish) or DELETE (English), matching athlete identity and current revision; end an active workout first. Test scripts use isolated data, never a user's default directory.

## Limits

Data lives on the computer running the local server, associated with the browser's HTTP-only cookie; it is not IndexedDB, an account or cross-device synchronization. Clearing browser cookies or using another browser profile changes local identity. Keep `FORMA_SIM_DATA` stable if configured. Defaults are now durable, but backups still require export. Legacy setup preferences remain a separate compatibility model; new goals and preferred duration are not yet adaptive prescriptions. Optional weight is a current manual profile value, not a measurement trend/history. Free-form limitation notes are stored, not automatically interpreted.

Only the profile panel has coherent TR/EN copy; full application localization remains open. Profile editing in immersive WebXR and physical Quest behavior are not verified. Do not call milestones B–F complete on the basis of profile persistence.
