# Ferrum v0.1.0 — Alpha pre-release

First source and binary release of the experimental LabAPI-hosted C# ↔ Rust bridge.

## Changes

- Small permanent LabAPI Loader and stable Contracts; immutable, versioned managed/native runtime with ABI v2.
- UTF-8 C# → Rust → C# round-trip and owned locale exchange, with explicit buffer ownership and error handling.
- CSV localization in English, Russian, Ukrainian and Simplified Chinese, separate framework/plugin language settings, and public localization contracts for future plugins.
- Direct server-console logging, localized orange banner, contributors and seasonal greetings.
- Background GitHub update preparation, configurable stable/pre-release channel, next-round full-restart scheduling and previous-runtime recovery.
- Windows x64 and Linux x64 runtime payloads plus standalone online installers.

## Installation

Stop your existing SCP:SL server and LocalAdmin. Download Ferrum.Installer-Win.exe or Ferrum.Installer-Linux and run interactively, or see [installation instructions](https://github.com/Ferrum-SCPSL/Ferrum/blob/v0.1.0/Ferrum/docs/installation.md). The installer downloads this release's runtime package from this repository and does not install the game or OS dependencies. Linux users must mark the installer executable first.

Main settings: LabAPI/configs/<port>/Ferrum/config.yml. Ferrum data: ~/.config/Ferrum by default on both OSes. Languages default to auto; the update channel defaults to stable, so future pre-releases require opting into stable_and_prerelease. Explicit installation of v0.1.0 is allowed regardless of the automatic update channel.

## Verification and limitations

Only compilation and static checks were authorized. No automated tests, installer executions, smoke programs or dedicated servers were run. Installation, updating and rollback are implemented but not confirmed end-to-end. Linux dedicated-server runtime is unconfirmed. No user Rust-plugin loader, gameplay event SDK, native unload or hot reload is included. SHA256SUMS.txt and release-manifest.json describe the assets.

Thanks to Narin for creating and supporting Ferrum.
