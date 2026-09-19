# Rollback guidance

## Current state

No driver package exists and no repository artifact should currently bind to Magic Mouse hardware. These requirements govern the future install and driver tasks.

## Required rollback properties

- Removing the enhancement must restore the standard Windows HID/mouse path.
- Rollback must not require the settings app or service to be operational.
- Service, virtual HID, filter binding, driver package, and persisted settings removal must have explicit and separately testable steps.
- A failed install or upgrade must not leave a partially bound filter.
- Recovery instructions must be usable with keyboard-only input and with a separate pointing device.
- Third-party filters or utilities must never be silently removed or modified.

## Pre-binding gate

Before the first hardware-binding test:

1. Create a Windows restore point, then record the original device stack and driver package state, including the Device Manager Driver Key and relevant `pnputil /enum-drivers` output.
2. Prepare offline or keyboard-accessible uninstall and recovery instructions.
3. Verify package removal on a non-critical test device or isolated test machine.
4. Verify pointer and button behavior after removal and reboot.
5. Record the exact package, OS build, Secure Boot state, and HVCI state.

Any base-input regression, incomplete package removal, or reboot-dependent unknown state blocks further distribution.

## Optional comparison-package recovery

If optional Apple Boot Camp mouse-driver research is performed, treat its installation and removal as a separate rollback exercise. Use an official 6.1.x source, retain the package locally rather than in this repository, identify the bound Apple INF before removal, and restore the original stack before project driver-binding work. The project must not require, bundle, redistribute, or modify the Apple package.
