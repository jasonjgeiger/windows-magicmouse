# MagicMouseWindows project plan

## Purpose

This directory is the planning source of truth for MagicMouseWindows, a Windows 11 touch-scrolling research project for supported Apple Magic Mouse hardware. The final architecture is intentionally undecided until the first descriptor dump and bounded captures are reviewed.

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
- A buildable user-mode solution skeleton exists for Shared, GestureCore, Service, and SettingsApp.
- Versioned capture and device-profile contracts have bounded serialization and invalid-contract tests.
- HidInspector and ReportRecorder provide safe enumeration and bounded, explicit raw-report capture.
- The non-binding VHF feasibility package builds a signable root-enumerated virtual wheel device; controlled test-signed installation remains manual.
- No physical-device filter, touch decoder, functional service, WinUI settings app, or installer has been created.
- No Magic Mouse model is currently declared supported.
- The first approved engineering scope is descriptor inspection and bounded protocol capture; planning beyond that is provisional.
- No Magic Mouse descriptor dump or touch capture has yet been collected.
- Production driver development is gated on repeatable hardware evidence and a confirmed need for kernel access.

## Documentation assessment

The planning set is internally aligned on version 1 scope, component boundaries, safety priorities, clean-room evidence, and release gates. The documentation gate remains open until the following conditions are met:

- The first project skeletons produce a clean build.
- Requirements are linked to initial test projects and evidence locations.
- Driver-specific build, install, uninstall, and rollback commands are verified before hardware binding.

Implementation evidence must not be inferred from these planning documents. Requirements and gates remain unverified until linked tests, captures, or manual results exist.

## Product summary

Version 1 is intended to provide:

- Vertical and horizontal touch scrolling.
- Independent enablement, speed, and reverse-direction settings for each axis.
- Normal pointer and button behavior when enhanced scrolling components are unavailable.
- Automatic recovery after Bluetooth reconnect, sleep, hibernation, service restart, upgrade, and reboot.
- A non-elevated Windows 11 settings experience after installation.

## Delivery gates

1. **Topology gate:** a descriptor dump establishes whether touch data is in a separately readable vendor-defined top-level collection or is held in the mouse collection.
2. **Protocol gate:** report decoding is supported by deterministic captures from the initial hardware target.
3. **Gesture gate:** fixture replay proves stable axis selection, scaling, reversal, and reset behavior.
4. **Driver gate:** normal pointer and button traffic remains unaffected during component failures.
5. **Integration gate:** scrolling and settings recover through device and operating-system lifecycle transitions.
6. **Distribution gate:** install, upgrade, repair, rollback, uninstall, signing, and compatibility requirements pass.

The topology gate selects the implementation direction: a safely readable separate vendor-defined collection permits a service-only feasibility path; otherwise, a narrowly scoped filter remains a hypothesis to validate. No driver-binding work may begin before this decision.

## Governing constraint

Implementation must be clean-room. It must not copy, redistribute, decompile, or depend on proprietary Magic Utilities or Apple binaries. Apple's Boot Camp `applewirelessmouse` driver and Magic Utilities are prior-art products that may be evaluated as end-user alternatives or compared only through public behavior; they are not implementation inputs. Interoperability research must be based on original, bounded hardware observations and documented provenance. Public specifications, Microsoft documentation and samples, and license-compatible source material are acceptable inputs.
