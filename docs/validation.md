# Validation strategy

## Principles

- Prefer deterministic fixture tests before hardware or kernel tests.
- Test the exact requirement at the narrowest reliable layer.
- Treat base-input preservation, rollback, malformed input, and lifecycle recovery as release gates.
- Record hardware, OS build, component versions, and profile ID with integration results.
- A support claim requires reproducible evidence, not a successful ad hoc trial.

## Traceability

Tests should reference requirement IDs from `requirements.md`. Release evidence should map every `must` requirement to at least one automated or documented manual result.

## Test levels

### Unit tests

- Device-profile selection and report validation.
- Parser byte ordering, contact counts, coordinates, and identity.
- Contact matching.
- Gesture transitions and reset reasons.
- Dead-zone and axis-lock boundaries.
- Scaling, reversal, fractional accumulation, and rate limiting.
- Settings validation and migrations.
- IPC encoding, size limits, and compatibility handling.

### Capture replay tests

- Golden fixtures with exact normalized contacts.
- Exact expected signed output deltas.
- Idle, slow, fast, diagonal, multi-contact, click, disconnect, and resume scenarios.
- Deterministic output across repeated runs.

### Property and fuzz tests

- Report IDs, lengths, contact counts, coordinates, flags, and truncation.
- Timestamp reversal, overflow, duplicates, and large gaps.
- IPC message versions, lengths, unknown fields, and malformed encodings.
- Settings numeric extremes and corrupted persisted data.

No fuzz input may reach physical system input without a bounded test boundary.

### Driver tests

- Hardware allowlist and profile mismatch.
- Input/control pass-through.
- IOCTL ACLs, lengths, state, and cancellation.
- Queue saturation and service crash.
- Device arrival, removal, surprise removal, reconnect, suspend, resume, hibernation, and shutdown.
- VHF descriptor and bounded report submission.
- Driver Verifier and supported static-analysis tools.
- Install, uninstall, and rollback.

### Service integration tests

- Per-device isolation.
- Async cancellation and backpressure.
- Driver/service version mismatch.
- Settings persistence and migration.
- Named-pipe authorization and malformed clients.
- Restart and recovery without losing base input.
- Diagnostic redaction and retention limits.

### UI tests

- Light, Dark, and representative Windows High Contrast themes.
- Keyboard-only navigation and focus order.
- Narrator names, roles, values, and state announcements.
- Long localized strings.
- Maximum supported text scaling.
- Display scaling at 100, 150, 200, and 250 percent.
- Connected, disconnected, unsupported, service-unavailable, and mismatch states.
- Operation without elevation.

### Application compatibility tests

- File Explorer and Windows Settings.
- Edge, Chrome, and Firefox.
- Microsoft Office applications.
- Visual Studio and Visual Studio Code.
- Representative Electron applications.
- Legacy Win32 scroll controls.
- Elevated applications.

For each application, test vertical, horizontal, slow, fast, reversed, and disabled-axis behavior where the application supports the axis.

### Installer tests

- Clean install.
- Upgrade with settings preservation.
- Repair.
- Uninstall.
- Failed-install rollback.
- Downgrade rejection.
- Driver/service version mismatch.
- Conflict detection without third-party modification.
- Secure Boot.
- Memory Integrity/HVCI.

## Hardware and OS matrix

Each result must identify:

- Magic Mouse model and profile.
- Hardware/compatible IDs and descriptor signature.
- Firmware observation where available.
- Bluetooth adapter and driver.
- Windows 11 edition, build, and architecture.
- Secure Boot and HVCI state.
- Power source and sleep model where relevant.

Initial minimum:

- Two Windows 11 x64 machines.
- At least two Bluetooth adapter implementations.
- Initial Magic Mouse 2 hardware connected over the observed HID transport; do not assume the Lightning port carries input data.
- Current supported Windows 11 release and one supported prior release when practical.

Other mouse models remain unclaimed until they independently satisfy the matrix.

## Acceptance scenarios

| ID | Scenario | Expected result |
|---|---|---|
| AC-001 | Stop service during pointer movement | Pointer and buttons continue; enhanced scrolling stops safely |
| AC-002 | Stop settings/tray app | Scrolling continues with last valid settings |
| AC-003 | Move inside dead zone | No scroll report |
| AC-004 | Mostly vertical gesture | Vertical output with no observable horizontal drift |
| AC-005 | Mostly horizontal gesture | Horizontal output with no observable vertical drift |
| AC-006 | Very slow gesture | Fractional motion eventually produces proportional output |
| AC-007 | Reverse one axis | Only that axis changes sign |
| AC-008 | Disable one axis | That axis emits no output; the other remains functional |
| AC-009 | Bluetooth reconnect | Session revalidates and scrolling resumes automatically |
| AC-010 | Sleep and hibernation cycles | Gesture state resets and scrolling resumes after revalidation |
| AC-011 | Unsupported report layout | Enhanced scrolling remains off; pointer/buttons remain functional |
| AC-012 | Driver/service mismatch | Explicit error, no enhanced output, base input preserved |
| AC-013 | Queue saturation | Enhanced data degrades safely without delaying base input |
| AC-014 | Elevated application | VHF scrolling behaves consistently with normal applications |
| AC-015 | Diagnostics export | Contains state/counters, not raw touch history |
| AC-016 | Upgrade and reboot | Valid settings and support status persist |
| AC-017 | Uninstall/rollback | Standard Windows mouse operation remains available |

## Performance measurement

Measure latency from service receipt of a timestamped driver frame to successful virtual-HID submission. Report p50, p95, p99, maximum, sample count, and test-machine load.

The initial release target is p95 below 16 ms under normal interactive load. Driver timestamp capture may be used for deeper analysis, but queue and clock boundaries must be documented before comparing measurements.

## Release gates

### Protocol gate

- Golden fixtures cover both directions on both axes.
- Lifecycle capture behavior is understood.
- Unknown/malformed reports fail explicitly.

### Gesture gate

- All deterministic and replay tests pass.
- Axis drift, reset, scaling, direction, and fractional behavior meet acceptance criteria.

### Driver safety gate

- Base input survives service loss and enhanced-path failures.
- Install, uninstall, and rollback pass.
- Verifier/static analysis have no change-related release blockers.

### Integration gate

- Reconnect, sleep, hibernation, restart, and version mismatch pass.
- IPC authorization and diagnostics privacy pass.

### Release gate

- Compatibility, accessibility, installer, Secure Boot, HVCI, and 24-hour lifecycle stress pass.
- Artifacts are signed through the selected supported path.
- Documentation accurately labels supported and experimental hardware.
