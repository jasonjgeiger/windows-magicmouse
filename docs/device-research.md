# Device protocol research

## Goal

Establish a clean-room, repeatable, evidence-based device profile for the initial Magic Mouse model before implementing production filter behavior.

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
- Device stack, Driver Key, and bound INF before and, when an optional comparison is used, after installing an Apple mouse-driver package.
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
11. When a lawful, locally installed Apple 6.1.x mouse-driver package is available, repeat steps 1 through 10 and compare only the externally observable descriptor, collection, report, and stack differences.

An Apple-driver comparison is optional feasibility research, not a runtime dependency or support criterion. Obtain packages only from Apple-controlled sources or tools that retrieve them from Apple's servers, retain them locally, and do not commit, redistribute, copy, or decompile their contents. Document package provenance and version, but treat undocumented driver behavior as an observation to reproduce independently rather than an implementation specification.

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
- Any descriptor, report, or stack differences observed with an optional Apple-driver comparison are independently captured and reproduced without depending on Apple binaries.
- Golden fixtures pass on at least two machines.
- Unknown or malformed layouts fail explicitly.
