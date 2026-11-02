# A/B local data inventory and release questions

Default behavior is offline. Product code makes no network requests and never opens a microphone. This does not establish that an imported SDK has no telemetry; inspect SDK configuration, merged manifests and device network traffic before release.

## Stored locally
- Phase 1 `forma-v1/profile.json`: selected workout movements, schema 1.
- Phase 1 `forma-v1/sessions/<uuid>.json`: session timestamps, outcomes, timed work/sets and self-reported repetitions/load, schema 1.
- New `forma-v1/product-profile.json`: schema 2, setup completion, coach appearance/personality, EN/TR setup preference, text-only voice, seated/standing selection, mute/volume/reduced-motion settings and optional goal/experience/duration/equipment/movement/exclusion preferences.
- User-triggered `forma-exports/setup-profile.json`: an explicit plaintext copy of the product profile only. It is not uploaded or a full session-history export.

No physical-space confirmation is stored as future permission. No motion recordings, precise equipment locations, camera frames, voice samples, identity/account tokens, diagnosis text or physiology estimates are collected by project code. Voluntary movement exclusions can still reveal sensitive context; treat them as personal data, minimize access, and explain their purpose.

## Retention, export and deletion
Retain data on this device until the user explicitly deletes it; no automated time-based retention is implemented. The current history screen's two-action Delete local data removes sessions, legacy profile, product setup and app-created export copies. Reset setup removes only product setup and clearly leaves session history separate. Export is invoked manually and requires ADB/local developer file access; consumer-accessible file sharing and full history export are unresolved. External copies/backups are outside app deletion. No secure erasure guarantee is made.

Files use OS sandbox access and plaintext JSON, not app-level encryption. Product profile saves use same-volume atomic replacement and reject overwriting malformed/newer data until explicit reset. No .NET harness data or real profiles are committed. Tests use temporary directories and synthetic fields.

## Release review, not a compliance checkbox
- Decide target age groups, supported markets, whether movement/fitness data receives special protection, and whether the proposed use triggers any health-product requirements. Obtain qualified review; no compliance certification is asserted.
- Publish application-specific privacy policy and terms with real operator/contact identity, purposes, categories, retention, deletion/export paths, device backups and SDK data flows. These documents are not drafted as legally approved text here.
- Review Android backup behavior, shared-headset use, local sandbox/encryption threat model and retention limits.
- Future cloud sync requires explicit informed opt-in, withdrawal, deletion propagation and backend retention; the contract has no implementation.
- Voice requires clear disclosure, permission, activation indication, stop control and retention decisions before requesting microphone access.
- Review Meta data-use/assessment requirements for each selected Platform SDK feature, particularly future purchases, identity and subscriptions. Local default storage does not exempt all platform obligations.

The [official Meta privacy policy requirements](https://developers.meta.com/vr/policy/privacy-policy/) require an accessible public HTTPS policy for applicable user/device data, accurate processing/purpose/deletion explanations and an up-to-date dashboard policy link. Confirm actual data flows before making the disclosure.
