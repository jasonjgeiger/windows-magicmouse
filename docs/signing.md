# Signing guidance

## Test signing

Test signing is only for controlled development machines. A test-signed package is experimental and must not be presented as production-ready or broadly distributed.

Before any test-signed driver binds to hardware:

- The hardware identity and descriptor signature are allowlisted.
- Base pointer and button pass-through has a documented test.
- Install, uninstall, and rollback steps have been exercised on a test machine.
- Recovery instructions remain available without relying on the affected mouse.
- Test certificates and private keys remain outside the repository.

Enabling Windows test-signing mode changes an operating-system security setting and must be an explicit developer action. Repository scripts must not silently enable it.

## Secure Boot cost

Test signing requires Secure Boot to be disabled, which weakens the security posture of the machine for as long as development continues. This is a real cost on a daily-driver machine.

- Prefer a dedicated test machine or a virtual machine with Bluetooth pass-through.
- If no such machine is available, treat that as a reason to evaluate a UMDF or user-mode design before committing to a kernel-mode driver.
- Record when Secure Boot was disabled and re-enable it when driver testing ends.

## Production signing

Production signing is a separate M7 deliverable. It requires the selected Microsoft-supported signing path, package provenance, release verification, Secure Boot and HVCI evidence, and all applicable release gates.

Passing a test-signed build does not imply production-signing eligibility.

## Artifact handling

- Never commit `.pfx`, `.pvk`, `.key`, `.pem`, or other private signing material.
- Keep certificate subjects, timestamps, hashes, and verification results with release evidence.
- Reject unsigned or unexpectedly signed production packages.
