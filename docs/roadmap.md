# Roadmap

## Status vocabulary

| Status | Meaning |
|---|---|
| Planned | Scope is documented but work has not started |
| In progress | Work is active and incomplete |
| Blocked | A named dependency or risk prevents progress |
| Validation | Implementation exists and is collecting gate evidence |
| Complete | Deliverables and gate evidence are present |
| Deferred | Intentionally outside the active release |

## Milestone overview

| Milestone | Scope | Entry condition | Exit condition | Initial status |
|---|---|---|---|---|
| M1 Foundation | Repository, solution boundaries, documentation, ADR | Planning approved | Clean skeleton build and documentation gate | In progress |
| M2 Protocol feasibility | Descriptor topology, inspection, recording, visualization, first device profile | M1 project skeleton | Topology and protocol gates pass on two machines | Planned |
| M3 Gesture engine | Deterministic state machine and replay tests | Valid first profile fixtures | Gesture gate passes | Planned |
| M4 Driver feasibility | Pass-through filter, secured transport, synthetic VHF | Protocol and architecture gates | Driver safety gate passes | Planned |
| M5 Integrated scrolling | Service, real frames, VHF output, settings persistence | M3 and M4 | Integration gate passes | Planned |
| M6 User experience | WinUI settings and tray access | Stable service IPC | UX/accessibility matrix passes | Planned |
| M7 Release candidate | Installer, hardening, signing, qualification | M5 and M6 | Full release gate passes | Planned |

## M1 - Foundation

Outcomes:

- Repository conventions and root documentation.
- Pinned toolchain after installed/current versions are verified.
- Buildable project skeletons.
- Requirements and validation traceability.
- ADR for filter + service + VHF.
- Clear test-signing and rollback guidance.

M1 does not include claims of device compatibility.

## M2 - Protocol feasibility

Outcomes:

- Safe device enumeration.
- Descriptor and transport topology evidence that selects service-only feasibility work or conditional filter evaluation.
- Bounded clean-room capture.
- Repeatable fixture format.
- Contact visualization.
- Documented initial device profile.
- Lifecycle evidence for reconnect and resume.
- Synthetic VHF evidence for vertical and horizontal output in normal and elevated applications.

If M2 cannot reliably identify contacts, direction, magnitude, and lifecycle boundaries, production filter development remains blocked and architecture options are reconsidered. If M2 proves a separately readable vendor-defined touch collection, service-only feasibility work takes precedence over any driver work.

## M3 - Gesture engine

Outcomes:

- Portable deterministic library.
- Dead zone and dominant-axis behavior.
- Independent axis settings.
- Fractional accumulation and rate limiting.
- Golden replay, boundary, and malformed-input tests.

Inertia and advanced gestures remain deferred.

## M4 - Driver feasibility

Outcomes:

- Test-signed pass-through filter restricted to the confirmed profile.
- Preserved base pointer/button traffic.
- Secured, bounded, cancel-safe frame transport.
- Synthetic relative Wheel and AC Pan output through VHF.
- Verified install, uninstall, and rollback.

Real touch-to-scroll integration is not required to prove the initial VHF path.

## M5 - Integrated scrolling

Outcomes:

- Per-device service sessions.
- Real frame decoding through GestureCore.
- Versioned driver/service communication.
- Persistent validated settings.
- Automatic reconnect, resume, and restart recovery.
- Bounded privacy-preserving diagnostics.

This milestone produces the first end-to-end experimental scrolling build.

## M6 - User experience

Outcomes:

- Non-elevated WinUI 3 settings app.
- Master and per-axis controls.
- Device and component health states.
- Light, Dark, and High Contrast support.
- Keyboard, Narrator, localization, and scaling readiness.
- Tray access independent of gesture processing.

## M7 - Release candidate

Outcomes:

- Repeatable install, upgrade, repair, rollback, and uninstall.
- Conflict detection.
- Secure Boot and HVCI results.
- Verifier, static analysis, stress, compatibility, and accessibility evidence.
- Appropriate production signing.
- Accurate supported/experimental hardware documentation.

## Post-v1 candidates

These items require separate requirements, risk review, and validation before scheduling:

- Additional Magic Mouse hardware profiles.
- ARM64.
- Inertia or momentum.
- Multi-finger gestures.
- User-facing advanced gesture tuning.
- Additional installer/distribution channels.

## Immediate execution order

1. Run one descriptor-topology inspection with `tools\Test-MagicMouse.ps1` or HidInspector and record the observed transport and collection layout.
2. Select a service-only feasibility path if a separate readable vendor-defined touch collection exists; otherwise retain filter evaluation as conditional.
3. Capture and validate the first hardware profile on the required matrix.
4. Run the non-binding synthetic VHF feasibility spike only if the selected path needs it.
5. Reassess architecture risks before production filter binding, gesture implementation, or integrated scrolling.

Production driver binding must not begin before the protocol feasibility gate passes.
