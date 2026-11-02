# ADR-004: Bounded daily rules before a larger workout library

Status: implemented in the shared C#/web path; Unity acceptance pending.
Date: 2026-11-02.

Use the existing reviewed two-movement catalog as a narrow allowlist. Daily time is an upper budget, never justification to repeat/lengthen unreviewed exercise volume. Unsupported strength is disclosed with an optional gentle alternative. High energy never changes dose. User-reported symptoms block all suggestions, including skipped check-in and quick choice. Space/profile restrictions combine using the stricter posture. All alternatives pass the same canonical-text/dose validator.

Read only valid completed records for context, counts and version-compatible comparisons. Recent participation is a heuristic with a clear explanation, not recovery/physiological measurement. Store no raw symptom/energy check-in by default. Do not create a backend, notification service or reward economy for this milestone.

Alternatives considered: fabricate longer strength programs (unsafe/unreviewed); ask a language model to decide safety (non-deterministic/offline failure); rewrite the workout engine (unnecessary risk). Keep the existing engine and add a small tested application policy instead.

Consequences: fewer sessions than the eventual commercial vision, but every recommendation is executable and its reason auditable. Future catalog expansions need qualified exercise/content review, tracking/capability validation and device tests. Beta hypotheses (start under ~2 minutes, 80% first completion, 40% seven-day return, satisfaction ≥4/5) remain unmeasured targets.

Future analytics specification only: opt-in, revocable event collection for app opened, first/second starts, completion/abandonment, safety pause, return after break and view transitions; aggregate counts and consent-scoped elapsed-to-start. Never upload raw motion, symptom check-ins or exact room geometry for general analytics. Define retention/deletion and minimum sample sizes before adding transport. No dashboard numbers or telemetry implementation are fabricated here.

Safety copy review references: [NHS dizziness](https://www.nhs.uk/symptoms/dizziness/) and [CUH exercise-safety guidance](https://www.cuh.nhs.uk/patient-information/cardiac-rehabilitation-home-exercise-guidelines/). Their symptom stop principles inform conservative blocking; no cardiac rehabilitation program, diagnosis or medical suitability assessment is implemented.
