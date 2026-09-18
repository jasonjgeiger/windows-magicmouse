# Contributing

## Safety and support claims

- Do not claim support for a device without the evidence required by `docs/device-research.md` and `docs/validation.md`.
- Do not bind driver code to hardware until allowlisting, base-input preservation, install, uninstall, and rollback requirements are satisfied.
- Treat service loss, device removal, suspend, resume, and malformed input as normal test cases.

## Clean-room protocol research

- Use original hardware observations, public specifications, Microsoft documentation and samples, or license-compatible source material.
- Do not copy, redistribute, decompile, or depend on proprietary Apple or third-party binaries.
- Record provenance, device identity, descriptor evidence, scenario, tool version, and fixture schema version.
- Capture only the explicitly selected device for a bounded duration or size.
- Review and remove unrelated or identifying data before sharing a fixture.

## Changes

- Keep requirement and task IDs stable.
- Link behavior changes to requirements and validation evidence.
- Add deterministic tests for parsers, contracts, and gesture behavior.
- Keep kernel-mode responsibilities narrow and bounded.
- Never commit certificates, private keys, raw diagnostics, crash dumps, or unreviewed captures.

## Validation

Run the narrowest build and test commands that cover the change. Hardware-dependent changes must include the device, descriptor signature, Windows build, Bluetooth adapter, component versions, and profile ID used for validation.
