# Magic Mouse hardware smoke test

## Current capability

This checkpoint can inspect Apple HID interfaces and capture bounded raw input reports from one explicitly selected readable interface. It does not decode touch contacts, install a filter driver, or generate scrolling from Magic Mouse input.

The synthetic VHF project is separate and never reads a physical device.

## Prerequisites

- Windows 11 x64.
- The Magic Mouse paired, connected, and visible in Windows Bluetooth settings.
- A PowerShell terminal opened at the repository root.
- No sensitive or unrelated activity during capture.

## Inspect

```powershell
.\tools\Test-MagicMouse.ps1
```

The command builds HidInspector and lists Apple HID interfaces only. Candidate labels are evidence hints, not compatibility claims.

At least one touch/vendor collection must report `CanOpenForRead` as `True` before raw report capture can proceed. The ordinary system mouse collection may be unreadable because Windows owns it; do not attempt to bypass that restriction.

## Capture

After reviewing the inspection output, copy the exact intended mouse or touch/vendor collection path. Start a ten-second, 2,000-frame bounded capture:

```powershell
.\tools\Test-MagicMouse.ps1 -Capture -AcknowledgeExplicitCapture -DevicePath '<exact path>'
```

During capture, use the mouse for the named scenario before the bound expires. The script never automatically selects a vendor-defined Apple interface because an unprofiled Apple keyboard or other device may expose a similar collection.

The fixture is written to `captures\initial-touch-smoke-test.json`, which is ignored by Git. A successful capture contains at least one report; a zero-report timeout fails explicitly.

## Safety and privacy

- Capture requires the explicit `-Capture` switch.
- Capture also requires the separate `-AcknowledgeExplicitCapture` confirmation.
- Every capture requires an exact, explicitly reviewed Apple device path.
- Keyboard, keypad, consumer-control, non-Apple, and unrelated HID usages are rejected.
- Duration, report count, report length, encoded document size, and text fields are bounded.
- No output report, pointer injection, driver binding, or system setting change occurs.
- Fixtures may contain stable hardware identifiers and raw touch reports. Review them before sharing.

## Expected next evidence

A useful initial result identifies:

- One or more Apple HID collections.
- Which collection is readable.
- VID, PID, usage page, usage, and input report length.
- Repeating raw reports while using the mouse.

Read-only capture may expose only standard mouse reports. Some Magic Mouse variants may require an initialization or feature-report sequence before touch frames appear; no such sequence will be sent until it is independently understood and bounded. The first device profile and touch decoder remain blocked until usable evidence is available and repeatable.

## Optional Apple-driver comparison

Apple's existing Magic Mouse driver can provide a useful comparison baseline, but it is not a project dependency, is not evidence that a model is supported, and must never be committed or redistributed with this MIT repository.

1. Before changing the system, create a Windows restore point and record the current device stack and installed driver packages. This also exercises the project's future rollback path.
2. Prefer an official Boot Camp Windows Support Software package in the 6.1.x line. On an Intel Mac, use **Boot Camp Assistant > Action > Download Windows Support Software**, following [Apple's guide](https://support.apple.com/en-ie/102465), and locate `BootCamp\Drivers\AppleWirelessMouse` on the resulting USB drive.
3. Without a Mac, `brigadier` from [timsutton/brigadier](https://github.com/timsutton/brigadier) may retrieve Boot Camp packages from Apple's servers on Windows. Its compatibility with current Apple catalogs has not been verified by this project. Unpack the resulting download locally with 7-Zip; do not use random package mirrors, especially for kernel-mode software.
4. Do not run the package-wide `Setup.exe`, which commonly refuses non-Apple hardware. Install only the mouse-driver package from `AppleWirelessMouse`, following its supplied installation instructions.
5. The 2009 Apple Bluetooth Update targets the original Magic Mouse; it is not a Magic Mouse 2 source. Magic Mouse 2 support arrived in Windows 10-era 6.x Boot Camp packages.

After the comparison package is installed, repeat the inspection and bounded capture procedures above. Compare descriptors, readable collections, and the device stack with the unmodified Windows state. Record the device's **Driver Key** from **Device Manager > Details** to identify the filter chain, and use `pnputil /enum-drivers` to identify the Apple INF that bound to the device. Preserve only externally observed metadata and captures that satisfy the privacy and provenance rules; do not copy, decompile, or redistribute Apple binaries or driver contents.
