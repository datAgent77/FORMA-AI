> Current delivery (2026-11-10): **Personal Coach V1, WebXR first** — persistent athlete profile, guided feedback-based workouts, explainable adaptation, actual-data progress and Studio/Forest. [Implementation, evidence, limitations and Quest launch](docs/personal-coach-v1/DELIVERY.md). Four movement animations and physical Quest validation remain outstanding. Unity remains paused.

```bash
FORMA_SIM_PORT=5097 bash tools/run-web-simulation.sh
```

Open [Personal Coach demo](http://127.0.0.1:5097/xr.html). The historical milestone notes below are retained for context; the current delivery report takes precedence.

> Visual revision (2026-11-06): holographic coach, curved glass studio, luminous floor and desktop bloom. [Delivery and verification](docs/holographic-studio/DELIVERY.md).

> Current milestone (2026-11-05): real-time rigged 3D coach, geometric studio and five-minute beginner workout. [Audit and plan](docs/webxr-3d/AUDIT-AND-PLAN.md) · [Verification and limitations](docs/webxr-3d/DELIVERY.md). Earlier photographic-demo delivery notes are historical. Unity development remains paused.

# FORMA AI
**Your Body. Your Coach. Your World.**

Original tagline: Your Real Workout. Your Virtual Universe.

Phase 0/1 source foundation for an offline, mixed-reality fitness studio on Meta Quest 3 and Quest 3S. Two selectable, equipment-free movements: gentle marching in place and shoulder rolls. Each has two 20-second sets, separated by 15-second rests. Selecting both gives a 135-second session including a 10-second introduction; no trailing rest.

## Current priority — WebXR first (2026-11-05)
Develop the existing WebXR experience first. **Unity installation is not required** for this development loop. The local server compiles the shared C# Domain/Product/Storage source only; it does not run Unity.

```bash
FORMA_SIM_PORT=5090 bash tools/run-web-simulation.sh
```

Open [WebXR Studio](http://127.0.0.1:5090/xr.html), complete setup, confirm clear space and start. The main button guides start/pause/resume; summary and local history stay on the same page. Set `FORMA_DOTNET` if .NET 8 is outside PATH. For a real Quest Browser/USB launch, see [WebXR setup](docs/WEBXR.md), using the same port. Desktop room/media are illustrative; physical headset behavior is not yet verified.

Native source and its blocked audit/build work remain preserved in [the native audit](docs/FORMA_NATIVE_XR_AUDIT.md). Native development is paused; browser results must never be reported as native builds or hardware validation. See [current WebXR delivery](docs/webxr-loop/DELIVERY.md).

## Phase 4 daily experience
The feature branch adds a shared deterministic daily check-in and recommendation card, history-backed greeting, quick choice, conservative safety exclusions and compatible alternatives. The current reviewed catalog remains 65–135 second equipment-free starters; 5/10/20/30 minutes are time budgets, not fabricated programs. A requested strength session is explicitly disclosed as unavailable. See [Phase 4 delivery](docs/phase4/DELIVERY.md).

## Phase 2 A/B additions
Offline first-use setup now has authored Unity presentation and tested product rules: EN/TR setup, original Sol/Mira/Atlas development coach presets, appearance/personality selection, optional fitness preferences, seated filtering, safety acknowledgement and versioned local profile save/export/reset. No new dependencies were installed. Hand adapter compilation/device acceptance, voice, production character art and Unity/device validation remain outstanding. Phase 1 workout cues stay English, as disclosed during setup.

See [Phase 2 delivery](docs/phase2/DELIVERY.md), [architecture](docs/phase2/ARCHITECTURE.md), [UX](docs/phase2/UX.md), [privacy](docs/phase2/PRIVACY.md) and [store readiness](docs/phase2/STORE-READINESS.md). Milestone C and later runtime work are gated on A/B acceptance.

## Browser simulation for testing
Run `bash tools/run-web-simulation.sh`, then open [localhost:5078](http://127.0.0.1:5078/). This is a local development test host using the actual shared C# workout/onboarding rules. It simulates XR signals and uses an illustrated coach; it does not validate Unity rendering or Quest safety. See [web simulation instructions and test evidence](docs/WEB-SIMULATION.md).

## Start here
1. Follow [Unity and dependency setup](docs/SETUP.md). Unity itself generates the project metadata.
2. Use `FORMA > Create Desktop Preview Scene` for a desktop logic/UI preview.
3. Install and configure Meta XR Core/OpenXR, then use `FORMA > Create Quest Studio Scene`.
4. Run tests, build the development APK and complete [hardware validation](docs/VALIDATION.md).

## Included
- Platform-independent workout and deterministic coach state machines.
- Exercise selection, introduction, work/rest intervals, pause/resume, abort and completion summary.
- Scene-generation source for a passthrough underlay, a stylized animated coach, visible written guidance, controller-ray/trigger buttons and a manually configured hand-pointer adapter and history.
- Fail-closed platform gate, manual clear-space confirmation, B/Y pause, pause-first exit confirmation.
- Local versioned profile and session files, interruption checkpoints, manual repetition/load logs, history deletion.
- Headset-free domain harness, Unity EditMode tests and GitHub domain CI.
- Interfaces only for future AI, body tracking, recognition, Worlds, Mirror, equipment, voice and analytics.

## Repository map
```text
AGENTS.md                    Engineering constraints
config/packages.required.json Reviewed dependency requests; not a resolved manifest
Unity/Assets/Forma/
  Domain/                    Pure C# workout, coach, safety and contracts
  Product/                   Pure C# profile, onboarding, catalog and service contracts
  Storage/                   Codec-injected product profile file repository
  Runtime/                   Presentation, input, composition and local persistence
  Integrations/Meta/         Isolated SDK adapter, opt-in compilation
  Editor/                    Scene generator and Android build command
  Scenes/                    Real scenes appear here after Editor generation
  Tests/EditMode/            NUnit domain and persistence tests
config/                      Dependency baseline
samples/                     Synthetic session example
tests/DomainHarness/        Standalone C# behavior checks
tools/                      Project creation, test and invariant commands
docs/                       Architecture, setup, safety and validation
.github/workflows/           Headset-free CI
```

## Test without Unity
Install .NET 8, then run:
```sh
dotnet run --project tests/DomainHarness/DomainHarness.csproj --configuration Release
dotnet run --project tests/ProductHarness/ProductHarness.csproj --configuration Release
python3 tools/validate-source.py
```
See [the validation report](docs/VALIDATION.md) for what was actually executed here. Unity tests and device builds are separate checks.

## Boundaries
No medical benefit, calorie, full-body tracking or automatic form-correction claims. No model calls, remote services, passthrough camera access, equipment tracking, Worlds or Mirror implementation. The avatar is a stylized guide with text cues; it is not a validated biomechanics demonstration. No audio or speech synthesis in Phase 1. See [safety](docs/SAFETY.md), [architecture](docs/ARCHITECTURE.md) and [third-party dependencies](THIRD_PARTY_NOTICES.md).

Original project source is not assigned a public redistribution license here; the owner must choose one before external distribution. Proprietary SDKs and Unity retain their own terms. Repository: [datAgent77/FORMA-AI](https://github.com/datAgent77/FORMA-AI). Phase 2 work remains on a feature branch; no automatic merge.
