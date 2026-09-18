# Protocol research contracts

## Purpose

The P2-01 contracts define bounded JSON documents for clean-room device profiles and capture fixtures. They are research and test formats, not a driver/service IPC contract.

The implementation is in `src\Shared\Protocol` under the `MagicMouseWindows.Contracts.Protocol` namespace and remains portable with no driver, service, storage, or UI dependency.

## Fixture document

A fixture contains:

- Schema version.
- Device-profile identity and descriptor signature hash.
- Scenario, device, Windows, architecture, adapter, tool, method, and provenance metadata.
- Ordered raw frames with monotonic relative microsecond timestamps and strictly increasing sequence numbers.
- Optional expected normalized contacts and expected scroll commands.

The schema intentionally has no user-name, machine-name, wall-clock timestamp, account, application-history, or unrelated-device fields.

## Device-profile document

A profile contains:

- Stable profile ID and schema version.
- Compatibility status.
- Explicit vendor/product, hardware, compatible ID, and descriptor-signature matches.
- Allowed report IDs with exact lengths.
- Contact and coordinate bounds.
- Initialization status.
- Decoded fields with Unknown, Hypothesis, or Confirmed confidence.

Profiles describe evidence. They do not grant driver-binding approval; binding additionally requires the protocol, pass-through, install, and rollback gates.

The current research signature is a SHA-256 hash over normalized HID identity, usage, report-length, and SetupAPI evidence. It is not represented as a hash of raw report-descriptor bytes. Future inspection work may replace or augment it with a raw descriptor hash when a documented, repeatable retrieval path is implemented.

## Bounds and rejection behavior

| Item | Limit |
|---|---:|
| Encoded document | 16 MiB |
| Frames per fixture | 250,000 |
| Bytes per report | 4,096 |
| Contacts per frame | 32 |
| Expected outputs per frame | 16 |
| Device matches per profile | 32 |
| Report shapes per profile | 32 |
| Decoded fields per profile | 128 |
| Text field | 512 characters |

Unknown JSON fields, comments, trailing commas, unsupported schema versions, invalid hashes, duplicate report IDs, out-of-range normalized values, non-monotonic timestamps, non-increasing sequences, and oversized values fail with an explicit contract path.

## Compatibility policy

- Readers accept only the current schema version.
- New optional or required fields require a schema-version increment because unknown fields are rejected.
- Migrations must be explicit and tested; silent interpretation of an unknown version is prohibited.
- Report bytes use JSON base64 encoding and retain the report ID as the first byte.
- Numeric JSON values use the invariant JSON representation defined by `System.Text.Json`.

## Validation

`tests\ProtocolFixtures` covers fixture and profile round trips plus unknown fields, unsupported versions, size bounds, null report data, malformed hashes, duplicate report IDs, and temporal ordering. Test metadata references the requirements exercised by each case.
