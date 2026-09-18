# Risk register

## Scoring

- **Likelihood:** Low, Medium, or High.
- **Impact:** Low, Medium, High, or Critical.
- Owners are roles until project contributors are assigned.
- A risk is closed only when evidence shows the trigger is no longer credible or the affected scope is removed.

| ID | Risk | Likelihood | Impact | Mitigation | Trigger / contingency | Owner |
|---|---|---|---|---|---|---|
| R-001 | Touch reports differ by model, firmware, or transport | High | High | Versioned profiles, descriptor signatures, evidence-based allowlisting | Create a distinct profile or keep the variant unsupported | Protocol |
| R-002 | Reconnect or resume requires undocumented initialization | Medium | High | Capture lifecycle traffic before driver work; model initialization explicitly | Block production binding until repeatable initialization is known | Protocol |
| R-003 | Filter defect disrupts normal pointer or button input | Medium | Critical | Pass-through-first implementation, bounded queues, fail-closed enhancement, early rollback tests | Stop release, disable binding, use documented rollback | Driver |
| R-004 | Horizontal/vertical wheel behavior varies by application | High | Medium | Standards-based Wheel/AC Pan output and compatibility matrix | Add application-specific findings; avoid unsafe injection workarounds | Integration |
| R-005 | Driver signing delays distribution | High | High | Separate user-mode/protocol progress from production signing; document test and production paths | Keep release experimental/test-signed until supported signing completes | Release |
| R-006 | Driver and service contract versions diverge | Medium | High | Version every message and reject incompatible peers explicitly | Disable enhanced output and request repair/upgrade | Driver/Service |
| R-007 | Gesture jitter causes accidental or diagonal scrolling | Medium | High | Dead zone, axis lock, fixture replay, hardware tuning | Adjust profile defaults with regression fixtures; do not add heuristics without evidence | Gesture |
| R-008 | Third-party filters or utilities conflict | Medium | High | Detect and explain conflicts; never silently remove software | Block install or disable enhancement with actionable guidance | Installer |
| R-009 | Reference implementation contaminates licensing | Low | Critical | Clean-room captures, public specs, Microsoft samples, provenance review | Remove contaminated work and recreate from permitted evidence | Project |
| R-010 | Malformed device input causes kernel memory or availability failure | Medium | Critical | Exact validation, bounded copying, fuzzing, verifier, minimal kernel parsing | Disable affected profile and ship corrective driver before support resumes | Driver/Security |
| R-011 | Diagnostics expose user activity | Low | High | Counters by default, explicit bounded captures, redaction review | Disable export/capture path until privacy issue is corrected | Service/Security |
| R-012 | Bluetooth adapters produce inconsistent timing or loss | High | Medium | Multiple-adapter matrix, discontinuity reset, monotonic timestamps | Tune reset policy by evidence; document incompatible adapters if necessary | Integration |
| R-013 | VHF output is rejected or behaves differently in elevated apps | Medium | High | Early synthetic VHF spike and elevated-app testing | Revisit descriptor/architecture before integrating full driver | Driver |
| R-014 | Settings corruption prevents service startup | Medium | Medium | Strict validation, atomic writes, backup/default recovery with explicit diagnostics | Quarantine invalid file and start with documented safe defaults | Service |
| R-015 | UI cannot satisfy High Contrast, scaling, or localization late in development | Medium | Medium | Use system controls/resources and test representative layouts each milestone | Reduce custom UI and correct resource/layout architecture before release | UI |
| R-016 | Scope expands into advanced gestures before scrolling is stable | High | Medium | Explicit v1 exclusions and gated roadmap | Defer request to post-v1 backlog unless it fixes a release blocker | Product |
| R-017 | Test fixtures are insufficient or non-reproducible | Medium | High | Canonical schema, scenario metadata, two-machine replay, provenance | Repeat captures; do not promote profile status | Protocol/Test |
| R-018 | Installer failure leaves the mouse or driver stack degraded | Medium | Critical | Transactional install design, restore points where appropriate, recovery media/instructions, repeated rollback tests | Halt distribution and provide targeted recovery procedure | Installer |

## Top feasibility risks

The first feasibility work after the M1 foundation should retire R-001, R-002, R-013, and R-017. These determine whether the selected architecture and initial hardware target are feasible before significant driver, service, or UI investment.

## Review cadence

Review risks:

- At each phase gate.
- After a new hardware/firmware variant appears.
- After any crash, verifier finding, rollback failure, privacy finding, or base-input regression.
- Before changing a hardware profile from Experimental to Supported.
- Before submitting production signing artifacts.

Every newly observed failure should either map to an existing risk and test or create a new risk, requirement, and regression case.
