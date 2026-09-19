# ADR-0001: Filter, service, and VHF architecture

- **Status:** Provisional, pending the descriptor gate
- **Date:** 2026-09-18

## Context

This decision rests on an unverified assumption: that Magic Mouse touch data rides in the same top-level collection as pointer and button data, which the Windows mouse stack claims exclusively. If that is true, no user-mode process can see the touch bytes, because Raw Input hands mice back as parsed `RIM_TYPEMOUSE` data, and a kernel or UMDF filter is required.

A single descriptor dump from real hardware settles this. If touch data is in a separate vendor-defined top-level collection that can be opened for read, the service-only alternative below applies and no filter is needed. Do not begin filter implementation before that dump is recorded.

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

Rejected as the primary architecture because injected input has trust, integrity-level, compatibility, and application-behavior differences, including elevated-window and UIAccess limitations. Those limitations may be acceptable for single-machine personal use, so it may be useful as a clearly isolated research comparison, but it must not become an undocumented fallback.

### Service-only direct device access

Not selected because exclusive or competing access to the relevant HID collections may be unreliable, may require unsupported assumptions, and does not by itself provide the desired standards-based output path. This option must be reconsidered if clean, non-exclusive access is proven without weakening base-input preservation.

### In-filter touch-to-wheel translation

A filter that converts touch movement directly into wheel and AC Pan output in-stream would remove both VHF and the Windows service. Reported behavior of Apple's own Boot Camp mouse driver suggests this is workable. It is not selected because it places gesture policy, scaling, and settings in kernel mode, which the design principles reject, and it makes per-axis settings and diagnostics harder to deliver.

Reconsider it if the service and VHF path proves fragile or if the project scope narrows to fixed-behavior personal use.

### UMDF filter via mshidumdf

A user-mode HID filter hosted by `mshidumdf` may provide the same access to touch reports as a KMDF filter while keeping faults out of the kernel. It has not been evaluated. Evaluate it before writing KMDF filter code if the descriptor gate shows a filter is required, and record the latency, lifecycle, and installation consequences.

### Adopt existing software instead of building

Apple's Boot Camp wireless mouse driver and the paid Magic Utilities already provide Magic Mouse scrolling on Windows. For personal use, evaluating them is cheaper than any option here. Record the evaluation result before continuing with this decision.

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

- The descriptor gate shows touch data in a separate, readable, vendor-defined top-level collection.
- A UMDF filter can access the same touch reports with lower failure impact than KMDF.
- Existing software already meets the goal for the intended use.
- VHF Wheel or AC Pan output is incompatible with required normal or elevated applications.
- A safe filter cannot preserve base input through service loss, removal, suspend, resume, or saturation.
- Installation or rollback cannot reliably restore the standard Windows device path.
- Required touch reports can be accessed safely and non-exclusively from user mode with a simpler supported architecture.
- The initial hardware cannot be identified and decoded with a strict, evidence-based profile.
