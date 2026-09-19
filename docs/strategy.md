# Product and engineering strategy

## Vision

Make Apple Magic Mouse touch scrolling feel like a reliable Windows input capability while preserving the behavior users already receive from the standard HID mouse path.

The product should behave as an enhancement, not as a replacement mouse stack. If the service, settings app, gesture engine, or virtual HID path fails, pointer movement and physical buttons must continue to work.

## Target users

- Windows 11 users who already use a Magic Mouse for pointing and clicking.
- Developers and technical users willing to test early hardware support.
- Later, general users who require signed installation, predictable upgrades, accessibility, and low-maintenance reconnect behavior.

## Initial target

- Operating system: Windows 11 x64.
- Hardware: Magic Mouse 2 over Bluetooth HID, pending descriptor and transport evidence. The Lightning port must not be assumed to carry HID data.
- Input features: direct one-finger vertical and horizontal scrolling.
- Configuration: independent axis enablement, speed, and direction.
- Distribution stage: developer test-signed builds only on an explicitly prepared test machine before any production release.

USB-C and original battery-powered models remain research targets until their descriptors and touch reports are shown to be compatible or receive separate device profiles.

## Strategic principles

### Preserve the native path

The filter forwards existing reports and control traffic unchanged. Enhanced scrolling fails closed without taking ownership of pointer movement or buttons.

### Keep policy out of kernel mode

Kernel code performs narrow validation, bounded transport, lifecycle handling, and virtual HID submission. Parsing policy, gesture recognition, settings, floating-point calculations, persistence, diagnostics, and UI remain in user mode.

### Require evidence before support claims

Hardware IDs alone are insufficient. A supported device profile requires a matching descriptor signature, documented report shape, lifecycle captures, fixture tests, and hardware acceptance results.

The first evidence also decides whether a kernel component is justified: a safely readable separate touch collection favors a service-only design; touch data held only by the mouse stack may justify evaluating a filter.

### Build deterministic behavior first

The gesture engine consumes timestamped normalized frames and produces deterministic output commands. Direct scrolling, reset behavior, and compatibility take priority over inertia or advanced gestures.

### Design recovery as a normal path

Bluetooth reconnect, surprise removal, suspend, resume, service restart, and version mismatch are expected states. Every component must expose an explicit transition and recovery behavior for them.

### Minimize collected data

Runtime diagnostics contain component versions, state, error codes, and bounded counters. Raw touch histories are not retained by default. Research capture requires explicit user initiation and produces bounded, reviewable fixtures.

## Version 1 scope

### In scope

- Allowlisted device discovery.
- Versioned device profiles and report decoders.
- Dead-zone and dominant-axis scrolling.
- Independent vertical and horizontal settings.
- Fractional movement accumulation.
- Relative HID Wheel and AC Pan output.
- Settings persistence and migration.
- Device/service status and diagnostics export.
- Test-signed development installation and complete rollback.

### Explicitly out of scope

- Inertia, momentum, pinch, rotate, swipe, or multi-finger shortcuts.
- macOS feature parity.
- Generic support for untested Apple pointing devices.
- Network services, accounts, analytics, or cloud settings.
- Circumventing Apple or third-party software protections.
- Production signing before protocol and driver gates pass.
- ARM64 before x64 stability is established.

## Success measures

| Area | Measure |
|---|---|
| Safety | Pointer movement and buttons remain usable when enhanced components stop or fail |
| Correctness | Golden captures produce exact normalized contacts and expected signed scroll deltas |
| Latency | Target p95 touch-frame receipt to virtual-HID submission is below 16 ms |
| Stability | No change-related Driver Verifier failures in the required stress runs |
| Recovery | Scrolling resumes without reinstall after reconnect, sleep, hibernation, or service restart |
| Efficiency | Idle operation is event-driven and effectively consumes no CPU |
| Privacy | Default diagnostics contain no raw input history |
| Compatibility | Vertical and horizontal output works in the defined Windows application matrix |

## Decision policy

When evidence conflicts with schedule, safety and evidence win. A model remains experimental, a feature is deferred, or the release is blocked rather than weakening allowlisting, validation, rollback, or base-input preservation.
