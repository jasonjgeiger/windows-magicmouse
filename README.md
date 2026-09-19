# MagicMouseWindows

MagicMouseWindows is an experimental Windows 11 project intended to add vertical and horizontal touch scrolling for explicitly supported Apple Magic Mouse hardware while preserving the standard Windows pointer and button path.

## Project status

This repository is in the foundation and protocol-research phase.

- No Magic Mouse model is currently supported.
- No production-ready driver or installer exists.
- Planning documentation is currently far ahead of hardware evidence. Documentation expansion is paused until the first descriptor dump and captures exist.
- Do not bind experimental driver code to hardware until the protocol and rollback gates pass.
- Developer builds will use test signing, which requires disabling Secure Boot on the test machine. Use a spare machine or VM, not a daily driver. Production signing is a separate release requirement.

## Initial scope

- Windows 11 x64.
- Magic Mouse 2 (Lightning charging port) as the first research target, connected over Bluetooth HID. The Lightning port is assumed to be charge and pair only; touch data is assumed to arrive over Bluetooth HID. Both assumptions are unverified.
- Direct one-finger vertical and horizontal scrolling.
- Independent enablement, speed, and direction settings for each axis.
- Deterministic behavior without inertia or advanced gestures.

## Next step: the descriptor gate

No further architecture, driver, or service work is approved until a descriptor dump from real hardware answers one question: is the touch data exposed in a separate vendor-defined top-level collection, or is it inside the collection the Windows mouse stack owns?

- Separate readable collection: a user-mode, service-only design may be sufficient and no kernel driver is required.
- Same collection as pointer/buttons: a kernel or UMDF filter is required, because Raw Input returns parsed `RIM_TYPEMOUSE` data and never exposes the touch bytes.

Run `.\tools\Test-MagicMouse.ps1` and record the result before committing to the architecture in [ADR-0001](docs/decisions/0001-filter-service-vhf.md).

## Prior art

Existing software already provides Magic Mouse scrolling on Windows. Evaluate it before investing in this project:

- Apple Boot Camp support software, which ships an Apple wireless mouse driver.
- Magic Utilities, a paid third-party utility.

This repository is a clean-room interoperability project. Prior art may be used as a functional benchmark and as a reason not to build, but it must not be decompiled, copied, or redistributed.

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
