# Implementation task backlog

## Task conventions

- IDs are stable and may be referenced by issues, commits, tests, and release notes.
- A task is complete only when its deliverables and verification are present.
- Tasks marked as gates block dependent production work.
- Hardware-dependent results must link to fixture or test evidence.

## Active execution set

This is the first bounded set of work to act on. Work outside this set remains planned until its dependencies and gate evidence are present.

| Order | Task | Immediate outcome | Status |
|---:|---|---|---|
| 1 | P1-01 | Repository conventions, support disclaimer, ignore rules, and verified toolchain inventory | Complete |
| 2 | P1-03 | Root documentation links and the missing documentation-gate guidance | Complete |
| 3 | P1-04 | ADR-0001 with alternatives, failure behavior, rollback, and reconsideration evidence | Complete |
| 4 | P1-02 | Buildable user-mode solution boundaries without hardware binding | Complete |
| 5 | P2-01 | Versioned capture/profile contracts with bounds, provenance, and invalid-schema tests | Complete |

The next bounded set is a single descriptor-topology inspection and bounded capture on the initial hardware. Driver binding, gesture implementation, settings UI, and installer work remain outside the active set until that evidence determines whether a kernel component is needed.

## Phase 1 - Repository and documentation baseline

### P1-01 Initialize repository conventions

**Depends on:** none

**Deliverables:**

- Initialize Git on the default branch.
- Add `.editorconfig`, `.gitignore`, `README.md`, license selection, and contribution guidance.
- Add `global.json`, shared build properties, and shared build targets only after tool versions are verified.

**Done when:**

- The repository opens from its root without relying on neighboring projects.
- Generated outputs, certificates, captures, and secrets are ignored appropriately.
- README support claims match the documented compatibility status.

### P1-02 Create solution and project skeletons

**Depends on:** P1-01

**Deliverables:**

- Create the solution and empty project boundaries for Driver, Service, GestureCore, SettingsApp, and Shared.
- Create test and tool project placeholders only where their build contract is known.

**Done when:**

- A clean restore/build succeeds for the created user-mode skeletons.
- Driver scaffolding does not claim to bind to hardware.

### P1-03 Establish baseline documentation

**Depends on:** none

**Deliverables:**

- Maintain the planning documents linked from `plan.md`.
- Add a root README with experimental-status, safety, support, and planning-document links.
- Add initial build/toolchain, test-signing, debugging, rollback, and threat-model guidance when the corresponding implementation surface is introduced.
- Keep packaging and production-signing guidance explicitly separate from developer test-signing instructions.

**Done when:**

- Documentation links resolve.
- Requirements have stable IDs.
- Every phase has entry and exit criteria.
- The root README makes no unsupported compatibility claim.
- Test signing is clearly distinguished from production signing.

### P1-04 Record architecture decision

**Depends on:** P1-03

**Deliverables:**

- ADR for filter + service + VHF.
- Alternatives covering `SendInput`, service-only access, and kernel gesture processing.
- Security, compatibility, failure, and rollback consequences.

**Done when:**

- The decision identifies evidence that could force reconsideration.

## Phase 2 - Protocol and output feasibility

### P2-00 Confirm transport and descriptor topology

**Depends on:** P2-02

**Deliverables:**

- Run `tools\Test-MagicMouse.ps1` or HidInspector against the initial hardware.
- Preserve a bounded descriptor dump with transport and top-level collection evidence.
- Determine whether touch data is in a separately readable vendor-defined collection or only in the mouse collection.
- Record whether Bluetooth HID, rather than the Lightning port, carries observed input.

**Done when:**

- The architecture decision records the observed topology.
- A service-only feasibility path is selected when safe non-exclusive access is proven; otherwise, filter evaluation remains conditional.

**Status:** Pending physical Magic Mouse hardware.

### P2-01 Define capture and profile schemas

**Depends on:** P1-02, P1-03

**Deliverables:**

- Shared types for descriptor identity, raw frame, timestamp, capture metadata, normalized contact, and profile identity.
- Versioned serialization with strict size limits.
- Redaction and provenance rules.

**Done when:**

- Round-trip and invalid-schema tests pass.
- No schema requires personally identifying or unrelated input data.

### P2-02 Build HidInspector

**Depends on:** P2-01

**Deliverables:**

- Enumerate candidate HID devices.
- Display hardware IDs, report descriptors, collection information, and safe metadata.
- Export deterministic descriptor evidence.

**Done when:**

- Unsupported devices are not opened for capture by default.
- Enumeration failures are explicit.

**Status:** Complete. The tool enumerates present HID interfaces without reading input and can filter/export Apple candidates.

### P2-03 Build ReportRecorder

**Depends on:** P2-00, P2-01, P2-02

**Deliverables:**

- Explicit device selection.
- Explicit start/stop.
- Duration and size bounds.
- Canonical fixture export.
- Cancellation and removal handling.

**Done when:**

- A disconnected device ends the capture cleanly.
- Bounds are tested and unrelated devices are excluded.

**Status:** Implementation complete; physical Magic Mouse validation is pending connected readable hardware.

### P2-04 Build ReportVisualizer

**Depends on:** P2-01

**Deliverables:**

- Fixture playback.
- Contact position and motion-vector display.
- Frame metadata and field annotations.
- No system input generation.

**Done when:**

- The same fixture produces the same visualization data.

### P2-05 Capture initial hardware matrix

**Depends on:** P2-00, P2-02, P2-03

**Deliverables:**

- Capture every scenario in `device-research.md`.
- Repeat key scenarios on two Windows 11 machines.
- Record reconnect and resume behavior.

**Done when:**

- Captures are bounded, anonymized, reviewed, and reproducible.

### P2-06 Implement first device profile

**Depends on:** P2-05

**Deliverables:**

- Document confirmed and unknown fields.
- Implement strict report validation and normalized-contact decoding.
- Add golden, truncation, mutation, and discontinuity fixtures.

**Done when:**

- Both axes and contact lifecycle decode consistently.
- Unknown layouts fail explicitly.
- The protocol feasibility gate in `device-research.md` passes.

### P2-07 Prove synthetic VHF output

**Depends on:** P2-00, P1-02, P1-04

**Deliverables:**

- A non-binding synthetic test path for relative vertical Wheel and horizontal AC Pan reports.
- A documented VHF descriptor and bounded submission contract.
- Results for normal and elevated applications in the initial compatibility set.

**Done when:**

- Both axes behave as expected without connecting the test path to Magic Mouse reports.
- Failures and application differences are documented before production filter work begins.
- R-013 has sufficient evidence to close, narrow, or force an architecture reconsideration.

**Status:** Driver and client implementation, package build, catalog signability, and INF validation are complete. Controlled test-signed installation and application-matrix behavior remain pending.

## Phase 3 - GestureCore

### P3-01 Define gesture contracts

**Depends on:** P2-01

**Deliverables:**

- Immutable normalized frame, settings, state, and output command types.
- Monotonic timing abstraction.
- Explicit reset reasons.

**Done when:**

- The library has no driver, service, storage, or UI dependency.

### P3-02 Implement deterministic state machine

**Depends on:** P3-01, P2-06

**Deliverables:**

- Idle, Tracking, ScrollingVertical, and ScrollingHorizontal states.
- Dead zone, axis lock, jitter suppression, and reset behavior.

**Done when:**

- Table-driven transition and boundary tests pass.

### P3-03 Implement output transformation

**Depends on:** P3-02

**Deliverables:**

- Independent enablement, speed, and reversal.
- Fractional accumulation.
- Bounded deltas and output rate limiting.

**Done when:**

- Exact-output tests pass for slow, fast, reversed, disabled, and rate-limited scenarios.

### P3-04 Add replay and property tests

**Depends on:** P3-03

**Deliverables:**

- Golden capture replay.
- Jitter, diagonal intent, report loss, frame-gap, setting-change, and reconnect cases.
- Property/fuzz coverage for normalized input boundaries.

**Done when:**

- Vertical fixtures have no meaningful horizontal drift and vice versa.
- Identical inputs always produce identical outputs.

## Phase 4 - Driver and virtual HID

### P4-01 Create non-binding KMDF skeleton

**Depends on:** P2-00, P1-02, P1-04

**Deliverables:**

- KMDF project and package layout based on supported Microsoft patterns.
- Tracing, version metadata, and static-analysis configuration.

**Done when:**

- The package builds but does not bind to unconfirmed hardware.

### P4-02 Implement pass-through filter

**Depends on:** P4-01, P2-06

**Deliverables:**

- Evidence-based hardware allowlist.
- Unchanged input/control forwarding.
- Install, uninstall, and rollback scripts/instructions.

**Done when:**

- Pointer and buttons pass hardware tests before touch extraction exists.
- Failed installation can be completely rolled back.

### P4-03 Add touch-frame transport

**Depends on:** P4-02

**Deliverables:**

- Validated frame extraction.
- Secured device interface and versioned IOCTLs.
- Bounded cancel-safe queues and counters.

**Done when:**

- Malformed requests, queue saturation, cancellation, removal, and service loss pass integration tests.

### P4-04 Add VHF output

**Depends on:** P4-03, P2-07

**Deliverables:**

- Relative vertical Wheel and horizontal AC Pan descriptor.
- Bounded submission contract.
- Production integration reusing the validated synthetic descriptor and submission behavior.

**Done when:**

- Both axes work in normal and elevated applications in the initial compatibility set.

### P4-05 Complete driver lifecycle handling

**Depends on:** P4-04

**Deliverables:**

- Surprise removal, reconnect, suspend, resume, shutdown, cancellation, and service-loss behavior.
- Driver Verifier and static-analysis configuration.

**Done when:**

- Base input remains available through tested enhanced-component failures.
- Required verifier and stress scenarios pass.

## Phase 5 - Windows service

### P5-01 Implement device sessions

**Depends on:** P4-03, P2-06

**Deliverables:**

- Interface discovery, per-device state, asynchronous reads, cancellation, and profile selection.

**Done when:**

- Multiple devices cannot mix frames or gesture state.

### P5-02 Integrate GestureCore and VHF submission

**Depends on:** P5-01, P3-04, P4-04

**Deliverables:**

- Frame-to-output pipeline.
- Backpressure and queue-saturation policy.
- Version negotiation.

**Done when:**

- Fixture and synthetic end-to-end tests produce expected HID commands.

### P5-03 Implement settings storage

**Depends on:** P3-01

**Deliverables:**

- Versioned schema under `%ProgramData%\MagicMouseWindows`.
- Validation, atomic writes, migration, and corruption recovery.

**Done when:**

- Valid settings survive required lifecycle events and invalid settings return explicit errors.

### P5-04 Implement local IPC

**Depends on:** P5-03

**Deliverables:**

- Access-controlled named pipe.
- Versioned status, settings, action, and diagnostics messages.
- Size, timeout, cancellation, and error contracts.

**Done when:**

- Unauthorized and malformed clients are rejected without affecting scrolling.

### P5-05 Implement recovery and diagnostics

**Depends on:** P5-02, P5-04

**Deliverables:**

- Device/service/driver lifecycle recovery.
- Bounded structured logs, counters, health state, and redacted export.

**Done when:**

- Scrolling recovers automatically and exports contain no raw history by default.

## Phase 6 - Settings and tray app

### P6-01 Create WinUI 3 MVVM shell

**Depends on:** P1-02, P5-04

**Deliverables:**

- Device, scrolling, startup, and diagnostics navigation.
- Explicit `{x:Bind}` modes and testable view models.

**Done when:**

- The app operates without elevation and handles service absence.

### P6-02 Implement settings surfaces

**Depends on:** P6-01

**Deliverables:**

- Master toggle.
- Vertical and horizontal cards with enable, speed, and reverse controls.
- Connected, disconnected, unsupported, and mismatch states.

**Done when:**

- Changes apply immediately and validation errors are actionable.

### P6-03 Complete accessibility and theming

**Depends on:** P6-02

**Deliverables:**

- Light, Dark, and High Contrast resources.
- Keyboard navigation, automation names, localization resources, text scaling, and long-string-safe layout.

**Done when:**

- The UI matrix in `validation.md` passes.

### P6-04 Add tray access

**Depends on:** P6-02

**Deliverables:**

- Open settings, status, and explicit service actions.

**Done when:**

- Scrolling continues when the tray process is stopped.

## Phase 7 - Packaging and release hardening

### P7-01 Build installer lifecycle

**Depends on:** P4-05, P5-05, P6-03

**Deliverables:**

- Install, upgrade, repair, uninstall, rollback, and conflict detection.
- No silent removal of third-party software.

**Done when:**

- Every lifecycle path is repeatable on clean test machines.

### P7-02 Validate platform security

**Depends on:** P7-01

**Deliverables:**

- Secure Boot and Memory Integrity/HVCI results.
- Driver static analysis, verifier, and PnP/power stress evidence.

**Done when:**

- No release-blocking findings remain.

### P7-03 Prepare production signing

**Depends on:** P7-02

**Deliverables:**

- Required HLK/dashboard artifacts and signing process.
- Artifact provenance and release verification.

**Done when:**

- Production artifacts use an appropriate Microsoft-supported signing path.

### P7-04 Run release qualification

**Depends on:** P7-03

**Deliverables:**

- Full acceptance, compatibility, lifecycle, accessibility, and 24-hour stress results.
- Known-issues and support-status documentation.

**Done when:**

- All release gates in `validation.md` pass.

## Deferred backlog

- Inertia and momentum.
- Additional gestures.
- USB-C Magic Mouse profile.
- Original battery Magic Mouse profile.
- ARM64 build and hardware validation.
- User-facing advanced dead-zone and axis-lock tuning.
- Additional virtual HID capabilities only when supported by validated scenarios.
