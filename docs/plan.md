# MagicMouseWindows project plan

## Purpose

This directory is the planning source of truth for MagicMouseWindows, a Windows 11 app and driver that provide native-feeling touch scrolling for supported Apple Magic Mouse hardware.

The project is greenfield. No hardware support is considered confirmed until descriptors, reports, lifecycle behavior, and gesture output have been captured and reproduced in tests.

## Planning documents

| Document | Purpose |
|---|---|
| [strategy.md](strategy.md) | Product direction, engineering principles, scope, and success measures |
| [requirements.md](requirements.md) | Traceable functional, reliability, security, UX, and performance requirements |
| [architecture.md](architecture.md) | Component boundaries, data flow, trust boundaries, and lifecycle behavior |
| [device-research.md](device-research.md) | Clean-room protocol research method and evidence requirements |
| [protocol-contracts.md](protocol-contracts.md) | Versioned capture/profile contracts, bounds, and compatibility policy |
| [hardware-smoke-test.md](hardware-smoke-test.md) | Safe inspection and bounded raw-report capture procedure |
| [tasks.md](tasks.md) | Ordered work packages, dependencies, deliverables, and completion criteria |
| [validation.md](validation.md) | Test levels, matrices, acceptance scenarios, and release gates |
| [risks.md](risks.md) | Risk register, mitigations, triggers, and contingency decisions |
| [roadmap.md](roadmap.md) | Milestones, sequencing, status definitions, and release progression |
| [development.md](development.md) | Verified toolchain, repository rules, build expectations, and debugging guidance |
| [signing.md](signing.md) | Separation of developer test signing and production signing |
| [rollback.md](rollback.md) | Pre-binding recovery requirements and rollback expectations |
| [threat-model.md](threat-model.md) | Assets, trust boundaries, threats, invariants, and security validation |
| [decisions/0001-filter-service-vhf.md](decisions/0001-filter-service-vhf.md) | Architecture decision and reconsideration evidence |

## Current status

- Planning and documentation are in progress.
- Planning output currently exceeds hardware evidence: seventeen planning documents exist and zero hardware captures exist. Documentation expansion is paused until that is corrected.
- A buildable user-mode solution skeleton exists for Shared, GestureCore, Service, and SettingsApp.
- Versioned capture and device-profile contracts have bounded serialization and invalid-contract tests.
- HidInspector and ReportRecorder provide safe enumeration and bounded, explicit raw-report capture.
- The non-binding VHF feasibility package builds a signable root-enumerated virtual wheel device; controlled test-signed installation remains manual.
- No physical-device filter, touch decoder, functional service, WinUI settings app, or installer has been created.
- No Magic Mouse model is currently declared supported.
- The next approved work is the descriptor gate below, not further planning.
- Production driver development is gated on repeatable hardware evidence.

## Descriptor gate

The architecture in [ADR-0001](decisions/0001-filter-service-vhf.md) assumes a kernel filter is required. That assumption is unverified and must be settled by one descriptor dump before any further architecture, driver, or service work.

Run `.\tools\Test-MagicMouse.ps1` (or HidInspector directly) against the target hardware and record which top-level collections exist, their usage pages and usages, their input report lengths, and whether each can be opened for read.

| Observation | Consequence |
|---|---|
| Touch data is in a separate vendor-defined top-level collection that can be opened for read | A user-mode, service-only design may be sufficient. No kernel driver is required. Reopen ADR-0001 under its existing reconsideration criteria. |
| Touch data shares the collection the Windows mouse stack owns | A filter is required. Windows parses that path into `RIM_TYPEMOUSE` Raw Input and never exposes the touch bytes to user mode. Continue with ADR-0001 or the UMDF alternative. |
| No touch data is visible at all under read-only capture | Investigate whether an initialization or feature-report sequence is needed before spending effort on either architecture. |

Until this gate produces a recorded result, treat the filter/service/VHF architecture as provisional.

## Prior art and build-versus-adopt

The following already provide Magic Mouse scrolling on Windows and must be evaluated before further investment:

- Apple Boot Camp support software, which includes an Apple wireless mouse driver.
- Magic Utilities, a paid third-party utility.

If the goal is personal use on one machine, testing these first is the correct first action and may close the project. If they are rejected, record why. They may be used as behavioral benchmarks only; decompiling, copying, or redistributing them is prohibited by the governing constraint below.

## Open assumptions to verify

| Assumption | Status | How to settle |
|---|---|---|
| Touch data reaches Windows over Bluetooth HID, and the Lightning port is charge and pair only | Unverified. Earlier documents described Lightning as the transport, which is likely wrong. | Inspect HID interfaces with the mouse connected wirelessly and again while cabled. |
| A kernel-mode filter is required to see touch bytes | Unverified | Descriptor gate above. |
| Touch-to-wheel translation must happen in user mode to keep policy out of the kernel | Design preference, not evidence | Compare against the in-filter translation alternative in ADR-0001. |
| Test signing is an acceptable development cost | Unverified for the intended machine | Test signing requires Secure Boot to be disabled. See [signing.md](signing.md). |

## Documentation assessment

The planning set is internally aligned on version 1 scope, component boundaries, safety priorities, clean-room evidence, and release gates. The documentation gate remains open until the following conditions are met:

- The first project skeletons produce a clean build.
- Requirements are linked to initial test projects and evidence locations.
- Driver-specific build, install, uninstall, and rollback commands are verified before hardware binding.

Implementation evidence must not be inferred from these planning documents. Requirements and gates remain unverified until linked tests, captures, or manual results exist.

New planning documents should not be added until the descriptor gate and the first hardware captures exist. Prefer correcting existing documents with observed evidence over writing additional ones.

## Product summary

Version 1 is intended to provide:

- Vertical and horizontal touch scrolling.
- Independent enablement, speed, and reverse-direction settings for each axis.
- Normal pointer and button behavior when enhanced scrolling components are unavailable.
- Automatic recovery after Bluetooth reconnect, sleep, hibernation, service restart, upgrade, and reboot.
- A non-elevated Windows 11 settings experience after installation.

## Delivery gates

0. **Feasibility gate:** prior art has been evaluated, and the descriptor gate result is recorded and has selected an architecture.
1. **Documentation gate:** requirements, architecture, security boundaries, validation, and rollback are documented.
2. **Protocol gate:** report decoding is supported by deterministic captures from the initial hardware target.
3. **Gesture gate:** fixture replay proves stable axis selection, scaling, reversal, and reset behavior.
4. **Driver gate:** normal pointer and button traffic remains unaffected during component failures.
5. **Integration gate:** scrolling and settings recover through device and operating-system lifecycle transitions.
6. **Distribution gate:** install, upgrade, repair, rollback, uninstall, signing, and compatibility requirements pass.

## Governing constraint

Implementation must be clean-room. It must not copy, redistribute, decompile, or depend on proprietary Magic Utilities or Apple binaries. Interoperability research must be based on original, bounded hardware observations and documented provenance. Public specifications, Microsoft documentation and samples, and license-compatible source material are acceptable inputs.
