# B–C verification gate — passed before D–F implementation

2026-11-10. Existing C# interval machine is reused behind `PersonalSessionEngine`; it is not replaced with a JavaScript timer. Six typed exercises have warm-up, exercise and cooldown blocks, explicit rest, safe pause/resume and mandatory manual feedback. Completing a timer does not infer repetitions or reported performance. Entirely skipped sessions do not count completed; cancelled/interrupted records remain separate. Feedback persistence failures pause, require an explicit retry and never duplicate reports.

`CoachMemoryService` reads persisted personal/legacy history. `AdaptiveTrainingEngine` implements bounded deterministic rules, recorded explanations and evidence session IDs. New beginner, three easy completions, repeated high difficulty, a recorded break and discomfort receive distinct decisions. Repeatedly requesting a plan does not increase its dose. A stale plan cannot bypass new evidence or discomfort. No LLM exists or is required.

- Release .NET build: zero warnings/errors.
- 63 web/API tests passed: [output](evidence/milestone-bc-tests.txt).
- Actual process restart/persistence/adaptation test passed: [output](evidence/milestone-bc-restart.txt). Its synthetic timer advancement and explicit test feedback are clearly labelled; these are not human workouts.
- Desktop Chrome used the existing saved test athlete, reviewed a mobility-specific 160-second plan with reasons, started it, reached its first feedback gate, submitted an explicit Partial/difficulty-3 report with repetitions left blank, then entered rest. No physical motion was measured.

Code: `Product/PersonalWorkout.cs`, `Product/AdaptiveTraining.cs`, `tools/WebSimulation/PersonalTrainingRepository.cs`, `PersonalCoachSimulation.cs`, `wwwroot/personal-coach-ui.mjs` and existing XR integration. Shared Product source remains under the historical Unity directory, but no Unity tooling is used.

Known limits at this gate: only march/shoulder procedural motion exists; other movements use explicit text instructions. Profile UI and manual feedback were desktop-tested; spatial feedback controls exist but are not Quest-verified. Progress dashboard, measurement history UI, full localization/presentation coverage and outdoor environment follow after this gate. No A–C test establishes completion of those features or hardware validation.
