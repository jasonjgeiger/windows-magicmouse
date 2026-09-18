# Architecture

## System context

```text
Apple Magic Mouse
        |
        | Bluetooth HID reports
        v
MagicMouseFilter.sys -----------------> Existing Windows HID/mouse path
        |                                pointer and buttons unchanged
        | validated touch frames
        v
MagicMouseService.exe
        | decode, normalize, gesture state,
        | settings, scaling, direction
        v
MagicMouseFilter.sys + VHF
        | relative Wheel and AC Pan reports
        v
Windows applications

MagicMouseSettings.exe <--- secured named pipe ---> MagicMouseService.exe
```

## Component boundaries

### Filter driver

Responsibilities:

- Attach only to confirmed, allowlisted Apple hardware.
- Forward existing HID input and control traffic unchanged.
- Validate relevant report IDs and lengths.
- Copy touch reports into bounded, cancel-safe queues.
- Expose a secured device interface to the service.
- Host a Virtual HID Framework device with relative `Wheel` and `AC Pan` usages.
- Handle PnP, power, cancellation, surprise removal, reconnect, and service loss.
- Record bounded counters and tracing events.

Prohibited responsibilities:

- Gesture recognition or model-selection policy.
- Floating-point scaling.
- Settings file or UI parsing.
- Network access.
- Raw touch-history persistence.
- Unbounded waits, queues, or allocations driven by device input.

### Windows service

Responsibilities:

- Discover filter interfaces and create one isolated session per device.
- Select a versioned device profile from hardware identity and descriptor evidence.
- Read frames asynchronously with cancellation.
- Decode and normalize touch contacts.
- Run the deterministic gesture state machine.
- Apply settings and submit bounded virtual HID commands.
- Persist validated settings atomically under `%ProgramData%\MagicMouseWindows`.
- Serve a versioned, access-controlled local named-pipe API.
- Recover from device, driver, service, and operating-system lifecycle changes.
- Produce privacy-preserving diagnostics.

### GestureCore library

Responsibilities:

- Define portable raw-frame, normalized-contact, settings, timestamp, and output types.
- Host device-independent gesture state.
- Implement dead zone, axis lock, scaling, reversal, fractional accumulation, and rate limiting.
- Expose deterministic reset behavior.
- Avoid Windows service, driver, storage, or UI dependencies so fixtures can run in unit tests.

### Settings app

Responsibilities:

- Show component and device status.
- Edit global and per-axis settings.
- Display validation and compatibility errors.
- Initiate diagnostics export and explicit service restart.
- Support Windows 11 theming, accessibility, localization, and text scaling.

The app is not part of the real-time input path and must not require elevation during normal use.

### Research tools

- **HidInspector:** enumerates candidate devices and records descriptors and safe metadata.
- **ReportRecorder:** captures explicitly started, bounded sessions into a documented fixture format.
- **ReportVisualizer:** displays decoded contacts and motion vectors without producing system input.

## Data contracts

All boundaries use explicit, versioned contracts:

- Driver to service: device identity, descriptor signature, raw frame bytes, monotonic timestamp, sequence, and status.
- Service to driver: virtual device identity and bounded relative scroll commands.
- App to service: settings reads/writes, component status, service actions, and diagnostics requests.
- Test fixtures: capture metadata, source profile, ordered frames, timing, annotations, and expected normalized/output data.

Each contract must define:

- Version and compatibility policy.
- Maximum encoded size.
- Required and optional fields.
- Numeric ranges and byte ordering.
- Cancellation and timeout behavior.
- Error codes and retry safety.
- Handling for unknown fields and versions.

## Gesture pipeline

1. Match an allowlisted hardware identity and descriptor signature.
2. Validate report identity and exact permitted length.
3. Decode the report through the selected device profile.
4. Validate and normalize coordinates and contact state.
5. Match contacts using contact IDs, or a documented deterministic fallback.
6. Ignore initial movement inside the dead zone.
7. Select the dominant axis after the threshold.
8. Suppress perpendicular jitter while axis-locked.
9. Apply axis enablement, speed, and direction.
10. Accumulate fractional movement.
11. Emit bounded relative HID reports under the rate cap.
12. Reset on contact lift, invalid input, discontinuity, long gap, removal, or suspend.

## Gesture states

```text
Idle
  -> valid contact appears
Tracking
  -> dead zone crossed with vertical dominance
ScrollingVertical
  -> reset condition
Idle

Tracking
  -> dead zone crossed with horizontal dominance
ScrollingHorizontal
  -> reset condition
Idle
```

Settings changes during a gesture must have deterministic semantics. Enablement and direction changes should take effect immediately; profile or structural changes should reset the active gesture.

## Trust boundaries

| Boundary | Primary controls |
|---|---|
| Device to driver | Hardware/profile allowlist, report ID and length validation, bounded copying |
| Service to driver | Device ACL, request validation, version negotiation, bounded queues |
| Settings app to service | Named-pipe ACL, caller authorization, schema validation, size limits |
| Disk to service | Versioned schema, atomic writes, strict validation, safe migration |
| Diagnostics export | Explicit action, redaction, bounded data, no raw history by default |

## Lifecycle behavior

| Event | Required behavior |
|---|---|
| Service absent or crashed | Base pointer/buttons continue; enhanced queues are drained or canceled |
| Settings app absent | Scrolling continues with last valid persisted settings |
| Device removed | Cancel I/O, discard queued frames, reset per-device gesture state |
| Device reconnected | Re-enumerate, revalidate profile, create a fresh session |
| Suspend | Stop new work, cancel or quiesce I/O, reset gesture state |
| Resume | Re-discover interfaces and resume only after profile validation |
| Driver/service mismatch | Reject enhanced protocol, report explicit status, preserve base input |
| Queue saturation | Drop or coalesce enhanced data according to policy; never block base input |
| Invalid report | Count and reject it; reset when continuity cannot be trusted |

## Proposed repository structure

```text
MagicMouseWindows.slnx
Directory.Build.props
Directory.Build.targets
global.json
README.md
LICENSE
.editorconfig
.gitignore
docs\
src\
  Driver\
  Service\
  GestureCore\
  SettingsApp\
  Shared\
tests\
  GestureCore.Tests\
  ProtocolFixtures\
  DriverIntegration\
  UiAutomation\
tools\
  HidInspector\
  ReportRecorder\
  ReportVisualizer\
installer\
  DriverPackage\
  Bootstrapper\
```

## Key architecture decision

The preferred design is a narrowly scoped HID filter plus user-mode service plus VHF output, rather than a `SendInput`-only implementation. This preserves a standards-based input path for normal and elevated applications, isolates gesture complexity in user mode, and allows base HID reports to continue independently.

This decision is recorded in [ADR-0001](decisions/0001-filter-service-vhf.md). Production driver behavior remains gated on its rollback, pass-through, protocol, and VHF feasibility evidence.
