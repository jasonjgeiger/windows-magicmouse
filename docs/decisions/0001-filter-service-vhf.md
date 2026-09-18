# ADR-0001: Filter, service, and VHF architecture

- **Status:** Accepted for feasibility work
- **Date:** 2026-09-18

## Context

Magic Mouse touch reports require device-specific validation and decoding, while Windows applications should receive standards-based relative vertical Wheel and horizontal AC Pan input. Existing pointer movement and physical buttons must remain available if any enhancement component fails.

The design must minimize kernel policy, support elevated applications, isolate per-device state, and permit complete rollback.

## Decision

Use a narrowly scoped KMDF HID filter for confirmed hardware, a Windows service for device profiles and deterministic gesture processing, and a Virtual HID Framework device for relative Wheel and AC Pan reports.

The filter will:

- Bind only to allowlisted hardware identities with verified descriptor evidence.
- Forward existing HID input and control traffic unchanged.
- Validate and copy only relevant touch frames through bounded, cancel-safe transport.
- Host or integrate the validated VHF descriptor and bounded submission path.

The service will decode profiles, normalize contacts, run GestureCore, apply settings, manage lifecycle recovery, and submit bounded relative output.

Production hardware binding remains blocked until protocol feasibility, synthetic VHF behavior, pass-through safety, installation, and rollback evidence exist.

## Alternatives

### SendInput from a user-mode process

Rejected as the primary architecture because injected input has trust, integrity-level, compatibility, and application-behavior differences. It may be useful only as a clearly isolated research comparison and must not become an undocumented fallback.

### Service-only direct device access

Not selected because exclusive or competing access to the relevant HID collections may be unreliable, may require unsupported assumptions, and does not by itself provide the desired standards-based output path. This option must be reconsidered if clean, non-exclusive access is proven without weakening base-input preservation.

### Gesture processing in kernel mode

Rejected because parsing policy, floating-point scaling, settings, and gesture state increase kernel complexity and failure impact without providing a necessary safety or latency benefit.

### Replace the standard mouse stack

Rejected because the project is an enhancement. Taking ownership of pointer and button behavior would violate the central failure-isolation requirement.

## Consequences

- The driver remains small but requires careful PnP, power, cancellation, queue, ACL, and signing work.
- Driver/service contracts and device profiles must be explicitly versioned and bounded.
- GestureCore can be deterministic and fixture-tested without Windows or hardware dependencies.
- Standards-based virtual HID output can serve normal and elevated applications if the feasibility evidence succeeds.
- Installation and rollback are release-critical because a filter participates in the device stack.

## Security and failure behavior

- Unsupported hardware or report layouts receive no enhanced output.
- Service loss drains or cancels enhanced work without blocking pointer or button forwarding.
- Protocol mismatch disables enhancement and exposes an explicit diagnostic state.
- Queue saturation drops or coalesces enhanced data according to a tested policy.
- Raw touch history is not retained during normal operation.

## Reconsideration criteria

Reopen this decision if evidence shows any of the following:

- VHF Wheel or AC Pan output is incompatible with required normal or elevated applications.
- A safe filter cannot preserve base input through service loss, removal, suspend, resume, or saturation.
- Installation or rollback cannot reliably restore the standard Windows device path.
- Required touch reports can be accessed safely and non-exclusively from user mode with a simpler supported architecture.
- The initial hardware cannot be identified and decoded with a strict, evidence-based profile.
