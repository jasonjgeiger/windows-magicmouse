# MagicMouseWindows

MagicMouseWindows is an experimental Windows 11 project intended to add vertical and horizontal touch scrolling for explicitly supported Apple Magic Mouse hardware while preserving the standard Windows pointer and button path.

## Project status

This repository is in the foundation and protocol-research phase.

- No Magic Mouse model is currently supported.
- No production-ready driver or installer exists.
- Do not bind experimental driver code to hardware until the protocol and rollback gates pass.
- Developer builds will use test signing; production signing is a separate release requirement.

## Initial scope

- Windows 11 x64.
- Magic Mouse 2 with Lightning as the first research target.
- Direct one-finger vertical and horizontal scrolling.
- Independent enablement, speed, and direction settings for each axis.
- Deterministic behavior without inertia or advanced gestures.

## Documentation

- [Project plan](docs/plan.md)
- [Product and engineering strategy](docs/strategy.md)
- [Requirements](docs/requirements.md)
- [Architecture](docs/architecture.md)
- [Device protocol research](docs/device-research.md)
- [Protocol research contracts](docs/protocol-contracts.md)
- [Magic Mouse hardware smoke test](docs/hardware-smoke-test.md)
- [Synthetic VHF wheel smoke test](driver/VhfWheelTest/README.md)
- [Implementation tasks](docs/tasks.md)
- [Validation strategy](docs/validation.md)
- [Risk register](docs/risks.md)
- [Roadmap](docs/roadmap.md)
- [Development environment](docs/development.md)
- [Signing guidance](docs/signing.md)
- [Rollback guidance](docs/rollback.md)
- [Threat model](docs/threat-model.md)
- [Architecture decisions](docs/decisions/)

## Build

The current solution contains non-functional user-mode project skeletons. It does not contain a driver or access hardware.

```powershell
dotnet restore
dotnet build --no-restore
```

To inspect a connected Magic Mouse without capturing input:

```powershell
.\tools\Test-MagicMouse.ps1
```

See the [hardware smoke-test guide](docs/hardware-smoke-test.md) before recording raw reports.

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) before submitting protocol evidence or implementation changes. Hardware observations must be bounded, provenance-documented, and free of unrelated input data.

## License

Licensed under the [MIT License](LICENSE).
