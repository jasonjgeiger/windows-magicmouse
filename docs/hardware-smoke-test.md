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
- The top-level collection layout and whether touch traffic is isolated in a separately readable vendor-defined collection.
- The observed input transport; do not assume the Lightning port carries HID data.

Read-only capture may expose only standard mouse reports. Some Magic Mouse variants may require an initialization or feature-report sequence before touch frames appear; no such sequence will be sent until it is independently understood and bounded. The first device profile and touch decoder remain blocked until usable evidence is available and repeatable.
