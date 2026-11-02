# Meta Horizon Store readiness — 2026-11-02

**Not ready for submission.** No Unity project has been imported/generated, no APK built, no device validation or store-app entitlement integration performed. This is a preparation checklist, not approval or a claim of policy compliance. No billing implementation or price exists.

## Confirmed from official documentation/search excerpts
- [Meta privacy policy requirements](https://developers.meta.com/vr/policy/privacy-policy/): applicable user/device data needs a public accessible HTTPS privacy policy accurately describing data, purposes and deletion; dashboard link must remain current.
- [Meta app entitlement guidance](https://developers.meta.com/vr/documentation/unity/ps-entitlement-check/): perform the app entitlement check within 10 seconds of launch; the documented Unity API is `Platform.Entitlements.IsUserEntitledToApplication()`. The development feature-tier provider in this repo is **not** that check and does not authorize a store purchase. Verify the complete current SDK flow and offline/error policy before release.
- [Meta asset guidelines](https://developers.meta.com/vr/resources/asset-guidelines/?intern_content=meta-quest-store-a-b-testing&intern_source=devblog): the indexed official guideline lists screenshots as 16:9, 2560×1440, 24-bit PNG. Recheck the entire current dashboard asset specification, icon/cover sizes, screenshot count and trailer formats at submission. These are not delivered assets.
- [Meta subscriptions documentation](https://developers.meta.com/vr/resources/subscriptions/) describes dashboard subscription tiers/periods and links subscription server APIs. Do not invent a checkout, infer a valid entitlement from a local tier flag, hardcode prices or assume a launch checkout alone establishes active status.

Several direct Meta documentation URLs returned no readable body in this environment. Search excerpts from official Meta pages were available for the entitlement/asset/subscription items above; the privacy policy page was readable in full. Items below need full current documentation/dashboard verification rather than a claim that every live requirement was confirmed.

## Account, identity and binary — verify in current dashboard
- [ ] Verified developer account/organization, required roles and distribution agreements. Check [developer verification policy](https://developers.meta.com/vr/policy/developer-verification/).
- [ ] Create/confirm the real store App ID and package identifier; development identifier is not the final release identity.
- [ ] Establish release keystore ownership, backup/recovery and CI secret handling; never commit private signing keys.
- [ ] Install supported packages, inspect actual package lock/toolchain and create a reproducible signed ARM64 build.
- [ ] Declare supported Quest 3/3S models/OS, Android API targets and permissions from actual build output.
- [ ] Implement real Platform SDK initialization and store-app entitlement behavior with approved test users/channels. Distinguish platform access control from optional Pro/Premium feature gating.

## Listing, content and consumer obligations — outstanding
- [ ] Store name, accurate description, category, locales, age/content/comfort and movement disclosures.
- [ ] Original/licensed production icon, covers, screenshots, trailer and any required promotional media; verify all current dimensions/counts/codecs from the [asset guide](https://developers.meta.com/vr/resources/asset-guidelines/).
- [ ] Use screenshots/trailer from the actual functioning product; do not present a render/mockup as a headset capture.
- [ ] Published privacy policy, terms, support contact, data inventory/deletion/export pathways and consumer rights review.
- [ ] Platform data-use checkup/assessment and SDK permission/telemetry disclosures as applicable.
- [ ] If subscriptions are later approved for implementation: official Meta purchase flows, current SKU/price retrieval, cancellation/renewal/refund/revocation handling, entitlement restoration and server-side trust boundaries. No safety/stop/exit/privacy control may be paid-only.

## Runtime validation and submission — outstanding
- [ ] Complete the current [VRC guidelines](https://developers.meta.com/vr/resources/publish-quest-req/) against actual binaries; record exact VRC IDs and dated evidence after reviewing the readable live guide.
- [ ] Device smoke tests for install/launch, input, passthrough, seated/standing setup, resume after interruptions, offline sessions, safe exit and data controls.
- [ ] Sustained CPU/GPU/frame timing, device refresh target, thermal and memory measurements on both target models. No numeric performance certification is invented here.
- [ ] Crash/hang, full-storage, headset removal, focus/tracking loss, corrupt profile, long frame gap and OS interruption tests.
- [ ] Production assets and all dependency licenses/attributions reviewed; development primitive coaches are not final commercial character art.
- [ ] Upload to controlled release channel, test with authorized accounts, complete dashboard metadata/technical checks, submit for review and resolve actual feedback. Store approval is never assumed.

## Backend boundary for later work
Client contains no AI keys, app secrets or receipt-validation secrets. A future backend would own provider credentials, rate/cost limits, identity verification, purchase/subscription event validation and consented sync/delete operations. It would validate recommended workout IDs/dose against the reviewed catalog before delivery. Keep the offline deterministic engine authoritative for safety-critical transitions. A/B does not deploy any backend.
