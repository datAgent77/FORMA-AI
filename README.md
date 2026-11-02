# FORMA AI
**Your Real Workout. Your Virtual Universe.**

Phase 0/1 source foundation for an offline, mixed-reality fitness studio on Meta Quest 3 and Quest 3S. Two selectable, equipment-free movements: gentle marching in place and shoulder rolls. Each has two 20-second sets, separated by 15-second rests. Selecting both gives a 135-second session including a 10-second introduction; no trailing rest.

## Status — read first
Unity is not installed on the authoring host. This repository contains authored C# source, a real-Editor scene builder, tests, and setup/build instructions. **It is not yet an Editor-generated Unity project, a compiled Unity application, or a hardware-validated experience.** No `.unity`, `.meta`, ProjectSettings, package lockfiles or APKs have been fabricated. The definition of success remains unverified until the Quest acceptance checklist passes.

## Start here
1. Follow [Unity and dependency setup](docs/SETUP.md). Unity itself generates the project metadata.
2. Use `FORMA > Create Desktop Preview Scene` for a desktop logic/UI preview.
3. Install and configure Meta XR Core/OpenXR, then use `FORMA > Create Quest Studio Scene`.
4. Run tests, build the development APK and complete [hardware validation](docs/VALIDATION.md).

## Included
- Platform-independent workout and deterministic coach state machines.
- Exercise selection, introduction, work/rest intervals, pause/resume, abort and completion summary.
- Scene-generation source for a passthrough underlay, a stylized animated coach, visible written guidance, gaze/trigger buttons and history.
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
dotnet run --projecttests/DomainHarness/DomainHarness.csproj --configuration Release
python3tools/validate-source.py
```
See [the validation report](docs/VALIDATION.md) for what was actually executed here. Unity tests and device builds are separate checks.

## Boundaries
No medical benefit, calorie, full-body tracking or automatic form-correction claims. No model calls, remote services, passthrough camera access, equipment tracking, Worlds or Mirror implementation. The avatar is a stylized guide with text cues; it is not a validated biomechanics demonstration. No audio or speech synthesis in Phase 1. See [safety](docs/SAFETY.md), [architecture](docs/ARCHITECTURE.md) and [third-party dependencies](THIRD_PARTY_NOTICES.md).

Original project source is not assigned a public redistribution license here; the owner must choose one before external distribution. Proprietary SDKs and Unity retain their own terms. No repository has been published or pushed remotely.
