# Requirements

## Conventions

- **Must** is release-blocking.
- **Should** is expected unless documented evidence justifies deferral.
- Requirement IDs remain stable after implementation references them.
- A requirement is complete only when its linked validation evidence passes.

## Functional requirements

| ID | Requirement |
|---|---|
| FR-001 | The system must provide vertical scrolling from supported touch reports. |
| FR-002 | The system must provide horizontal scrolling from supported touch reports. |
| FR-003 | Users must be able to enable or disable scrolling globally. |
| FR-004 | Users must be able to enable or disable each axis independently. |
| FR-005 | Users must be able to configure speed for each axis independently. |
| FR-006 | Users must be able to reverse direction for each axis independently. |
| FR-007 | Settings changes must apply without reinstalling or reconnecting the mouse. |
| FR-008 | Settings must survive service restart, reboot, upgrade, reconnect, sleep, and hibernation. |
| FR-009 | The settings app must display device connectivity and driver, service, and app versions. |
| FR-010 | The settings app must expose diagnostics export and explicit service restart actions. |
| FR-011 | Unsupported or unrecognized report layouts must disable enhanced scrolling for that device. |
| FR-012 | The system must support multiple connected allowlisted devices without mixing their gesture state. |

## Input-preservation and lifecycle requirements

| ID | Requirement |
|---|---|
| LR-001 | Existing pointer movement and physical button reports must be forwarded unchanged. |
| LR-002 | Pointer movement and buttons must continue when the service or settings app is stopped or crashes. |
| LR-003 | Queued reads and writes must be cancelable during removal, shutdown, and service disconnect. |
| LR-004 | Each device session must reset gesture state on removal, suspend, invalid reports, discontinuity, or excessive frame gap. |
| LR-005 | The system must recover automatically after Bluetooth reconnect and operating-system resume. |
| LR-006 | Driver/service protocol incompatibility must produce an explicit error and no enhanced output. |
| LR-007 | Driver queues must be bounded and must not block the underlying mouse report path. |
| LR-008 | Installation must support complete rollback to the standard Windows device path. |

## Gesture requirements

| ID | Requirement |
|---|---|
| GR-001 | No scroll output must be emitted before movement crosses the configured dead zone. |
| GR-002 | Dominant-axis selection must use a documented ratio, initially 1.25:1. |
| GR-003 | Perpendicular jitter must be suppressed while an axis is locked. |
| GR-004 | Disabled axes must produce no output while preserving other gesture state correctly. |
| GR-005 | Speed must be applied independently after axis selection. |
| GR-006 | Direction reversal must invert only the selected axis output. |
| GR-007 | Fractional movement must accumulate so slow intentional motion is retained. |
| GR-008 | Output must be bounded and rate-limited, initially to at most 125 Hz. |
| GR-009 | Gesture processing must be deterministic for identical frames, settings, and timestamps. |
| GR-010 | Version 1 must not synthesize inertia after contacts lift. |

## Security and privacy requirements

| ID | Requirement |
|---|---|
| SR-001 | The driver must bind only to explicitly allowlisted hardware identities and verified profile signatures. |
| SR-002 | All report and IOCTL lengths, identifiers, counts, and numeric ranges must be validated before use. |
| SR-003 | Driver-service and app-service endpoints must restrict access to the intended local principals. |
| SR-004 | Every IPC contract must be versioned and have explicit maximum message sizes. |
| SR-005 | Settings must be validated before persistence or application. |
| SR-006 | Settings writes must be atomic and recoverable from interruption. |
| SR-007 | Diagnostics must not contain raw touch history by default. |
| SR-008 | Capture tools must require an explicit start action and enforce bounded capture duration or size. |
| SR-009 | Production updates and packages must have verifiable integrity and an authenticated signing chain. |
| SR-010 | Kernel-mode code must not perform network access, settings parsing, or unbounded allocation based on device input. |

## Performance requirements

| ID | Requirement |
|---|---|
| PR-001 | Target p95 touch-frame receipt to scroll submission latency must be below 16 ms under normal load. |
| PR-002 | Idle processing must be event-driven and effectively consume no CPU. |
| PR-003 | Queue saturation must degrade enhanced scrolling without delaying base mouse input. |
| PR-004 | Diagnostic logging must be bounded by size and retention. |

## Settings defaults

| Setting | Default | Validation |
|---|---:|---|
| Scrolling enabled | On | Boolean |
| Vertical enabled | On | Boolean |
| Horizontal enabled | On | Boolean |
| Vertical speed | 1.0 | 0.25 through 4.0 |
| Horizontal speed | 1.0 | 0.25 through 4.0 |
| Reverse vertical | Off | Boolean |
| Reverse horizontal | Off | Boolean |
| Dead zone | Profile/internal default | Positive bounded value |
| Axis-lock ratio | 1.25 | Greater than 1.0 and bounded |
| Output rate cap | 125 Hz | Positive bounded integer |

Advanced tuning values should remain internal until hardware evidence supports safe user-facing ranges.

## UX and accessibility requirements

| ID | Requirement |
|---|---|
| UX-001 | The installed settings app must operate without elevation except for explicit install or repair actions. |
| UX-002 | The app must represent connected, disconnected, unsupported, service-unavailable, and version-mismatch states. |
| UX-003 | All controls must be keyboard reachable and expose meaningful automation names. |
| UX-004 | Layout must remain usable with long localized strings and maximum supported text scaling. |
| UX-005 | Light, Dark, and High Contrast themes must use system-aware resources. |
| UX-006 | Binding modes and update behavior must be explicit. |
| UX-007 | The tray process must not own gesture processing or be required for scrolling. |
