# Device protocol research

## Goal

Establish a clean-room, repeatable, evidence-based device profile for the initial Magic Mouse model before implementing production filter behavior.

## First evidence: the descriptor gate

Before any scenario captures, record a descriptor dump for the target device and answer one question: is touch data exposed in a separate vendor-defined top-level collection, or is it inside the collection the Windows mouse stack owns?

Record for each top-level collection: usage page, usage, input report IDs and lengths, and whether the collection can be opened for read.

- Separate readable vendor collection: a user-mode, service-only design may be sufficient. Reopen [ADR-0001](decisions/0001-filter-service-vhf.md).
- Shared with the mouse collection: a filter is required, because Raw Input returns parsed `RIM_TYPEMOUSE` data and never exposes touch bytes.
- No touch data visible: determine whether an initialization or feature-report sequence is required before choosing an architecture.

Also record the transport used. Magic Mouse 2 is assumed to deliver touch data over Bluetooth HID, with the Lightning port serving charging and pairing only. Confirm or correct that here.

## Evidence standard

A field is **confirmed** only when:

- Its behavior is repeatable across multiple captures.
- Controlled physical input changes it predictably.
- Its interpretation is consistent across at least two Windows 11 test machines.
- A fixture reproduces the same parser output without connected hardware.

Unconfirmed bytes must be labeled as hypotheses or unknown. They must not control kernel allocation, indexing, report forwarding, or support claims.

## Capture environment

Record for each session:

- Device model and visible identifiers.
- Hardware and compatible IDs.
- Transport and descriptor hashes.
- Firmware observations available through documented interfaces.
- Windows edition, build, architecture, and Bluetooth adapter.
- Tool version and fixture schema version.
- Test scenario and operator annotation.

Do not record user identity, unrelated keyboard input, unrelated device traffic, or open-ended background sessions.

## Required scenarios

1. Idle with no contacts.
2. Contact down without intentional movement.
3. Slow and fast vertical movement in both directions.
4. Slow and fast horizontal movement in both directions.
5. Diagonal movement with different dominant axes.
6. Multiple simultaneous contacts.
7. Contact lift and rapid re-contact.
8. Physical click without movement.
9. Click-drag with touch movement.
10. Malformed/truncated fixture mutations.
11. Bluetooth disconnect and reconnect.
12. Sleep, resume, hibernation, and resume.
13. Service/tool stop while the device remains connected.
14. Device power cycle where the model permits it.

## Capture workflow

1. Enumerate devices and save descriptors and safe metadata.
2. Select one explicit device instance.
3. Start a bounded recording with a named scenario.
4. Perform only the documented physical action.
5. Stop recording immediately.
6. Review the capture for unrelated traffic and redact or discard it.
7. Convert the capture into the canonical fixture format.
8. Add annotations separately from observed bytes.
9. Replay the fixture through the parser.
10. Compare results across repeated sessions and machines.

## Device profile contents

Each profile must define:

- Stable profile ID and schema version.
- Allowlisted hardware/compatible IDs.
- Descriptor signature or hash policy.
- Relevant report IDs and exact permitted lengths.
- Required initialization or feature-report sequence, if confirmed.
- Byte ordering and bit-field definitions.
- Coordinate ranges, units, origin, and orientation.
- Contact count and contact identity rules.
- Confidence state for every decoded field.
- Discontinuity and reset rules.
- Known firmware or transport variations.

## Compatibility status

Use these states consistently:

| Status | Meaning |
|---|---|
| Unknown | No usable capture evidence |
| Captured | Fixtures exist but interpretation is incomplete |
| Decoded | Required fields are repeatably decoded |
| Parser tested | Golden fixtures and malformed-input tests pass |
| Hardware tested | Required scenarios pass on physical hardware |
| Experimental | Available only with explicit warning |
| Supported | All release gates for that model pass |
| Blocked | A documented incompatibility or safety issue prevents progress |

## Fixture requirements

- Deterministic and reviewable format.
- Monotonic relative timestamps rather than wall-clock identity data.
- Bounded length and file size.
- Source profile and scenario metadata.
- Raw report bytes only from the selected device.
- Optional expected normalized contacts and output commands.
- License and provenance statement.
- No unrelated input history.

## Protocol feasibility gate

Production driver work remains blocked until:

- Direction and relative magnitude decode consistently for both axes.
- Contact start, continuation, and lift are distinguishable.
- Report lengths and identities are bounded and documented.
- Reconnect and resume initialization behavior is understood.
- Golden fixtures pass on at least two machines.
- Unknown or malformed layouts fail explicitly.
