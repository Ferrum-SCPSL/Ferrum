# Update lifecycle

Only public releases from Ferrum-SCPSL/Ferrum are used; no GitHub token is required. After readiness and then every configured interval, Updater checks in the background. Stable filters out GitHub pre-releases and semantic prerelease tags; stable_and_prerelease includes them. Version comparison follows semantic precedence. Equal/older versions and the most recently rejected runtime are not applied automatically.

Downloads use the release's platform archive and SHA256SUMS.txt, then verify its internal manifest and every file hash. SHA-256 detects transfer/corruption errors; the trust source is HTTPS and the official GitHub release, not an independent publisher signature. ZIP/TAR extraction rejects traversal, duplicates, unsupported links and excessive files/size. Platform, ABI 2 and required bootstrap/contracts protocols are checked before staging can be committed.

The complete immutable runtime is installed under a Ferrum-root install lock. Per-port runtime.json is updated atomically under its own lock; the installation ID derives from canonical LabAPI path plus port. Files currently used by the process are never replaced. The previous runtime remains available. User configs, catalogs and plugins are not rewritten.

When a verified pending version is ready and restart_after_update is true, the natural RoundEnding event schedules ServerStatic.StopNextRound = Restart on the game thread. It does not force the current round to end. An administrator's existing non-DoNothing action is preserved. With no completed round, the update waits; no timer forcibly ends the game. A full process restart requires the server's normal LocalAdmin/supervision restart environment. Ferrum does not modify panel settings or supervisor configuration.

On the next full start, bootstrap records an attempt before loading the selected version. Readiness commits it as active and remembers the previous version. Failed/incomplete attempts are rejected and select the previous available version on the next start. There is no same-process retry after loading a failed payload. A first installation with no working previous version remains disabled until repaired. An explicit stopped-server reinstall clears rejection, and installer --rollback selects the previous validated version.

Network, validation or disk failures leave the current version active and do not schedule a restart. A release requiring a newer bootstrap/contracts protocol produces a localized installer-required message instead of a pending runtime. Such upgrades require stopping the server and running the newer installer. This updater never updates SCP:SL, LabAPI or third-party plugins.

This lifecycle has been inspected in code and compiled, but has not been executed end-to-end.
