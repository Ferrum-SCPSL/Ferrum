# Verification status for v0.1.0

Policy: compilation and static artifact/source inspection only. No automated tests, smoke executables, installers or dedicated servers are executed locally or in Actions.

Performed locally during implementation:

- Managed Release compilation, no warnings or errors.
- Windows x64 production Rust Release compilation.
- cargo fmt and cargo clippy --lib with warnings denied; no test execution.
- Self-contained installer publish for Windows x64 and Linux x64; neither executable launched.
- Static PE/ELF/CSV, archive-membership and SHA-256 checks during packaging.
- Source inspection of update staging, atomic state transitions, rejection/rollback, config preservation and holiday boundaries.

Windows/Linux Debug and Release CI build results and complete published release assets are checked as the delivery step. Build status is visible in GitHub Actions. A successful build is not evidence of a successful install or live update.

Not verified by execution: installer setup/migration/rollback, update download/switch/restart/recovery, new ABI locale calls, current dedicated-server bootstrap, multi-instance behavior and server-side Linux runtime. Linux support remains a build target until a real Linux dedicated-server run confirms it.

Historical results for the old ABI v1 PoC are not acceptance evidence for this revision. The former managed smoke sources were deleted by the owner in GitHub while implementation was in progress; that deletion is preserved in the current tree and historical sources remain in Git history.
