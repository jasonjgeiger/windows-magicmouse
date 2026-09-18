# Threat model

## Protected assets

- Kernel integrity and system availability.
- Existing pointer and physical-button input.
- The integrity of driver, service, settings, and update artifacts.
- Local settings and diagnostics.
- User privacy, including touch and interaction history.

## Trust boundaries

| Boundary | Untrusted or partially trusted input | Required controls |
|---|---|---|
| Device to driver | HID reports, descriptors, timing, removal | Identity/profile allowlist, exact lengths, bounded copying, cancellation |
| Service to driver | IOCTL messages and output requests | ACLs, versioning, maximum sizes, state validation, bounded queues |
| App to service | Settings and diagnostic requests | Named-pipe ACLs, caller authorization, schema validation, timeouts |
| Disk to service | Settings and migration data | Strict schema, numeric bounds, atomic writes, safe recovery |
| Capture/diagnostics export | Raw frames, metadata, logs | Explicit action, bounds, redaction, provenance, no raw history by default |
| Build and release | Packages, symbols, signing inputs | Reproducible builds, secret isolation, authenticated signing, verification |

## Primary threats

- Malformed reports causing out-of-bounds access, excessive allocation, hangs, or kernel failure.
- An unauthorized local process controlling virtual HID output or settings.
- Driver/service version confusion producing unsafe parsing or output.
- Queue saturation delaying the standard mouse path.
- A failed lifecycle transition leaving stale gesture state or blocked I/O.
- Diagnostics or fixtures exposing unrelated user activity.
- A malicious or corrupted package, update, settings file, or device profile.
- Overbroad hardware matching binding the filter to an unsupported device.

## Security invariants

- Pointer and button forwarding does not depend on the service, UI, gesture engine, or virtual HID path.
- Unknown identities, report shapes, protocol versions, and settings fail explicitly with enhanced output disabled.
- Device-driven sizes and counts are validated before allocation, indexing, or copying.
- Queues, messages, deltas, logging, captures, and waits are bounded.
- Kernel-mode code performs no network access, settings parsing, or gesture policy.
- Raw touch history is not retained in normal operation.
- Signing secrets never enter the repository.

## Security validation

Security-relevant tests are defined in `validation.md` and must cover malformed device input, malformed IPC, ACLs, cancellation, saturation, service loss, device removal, rollback, diagnostics redaction, Driver Verifier, static analysis, Secure Boot, and HVCI.

The threat model must be updated when a new trust boundary, privileged operation, external dependency, update mechanism, or hardware profile is introduced.
