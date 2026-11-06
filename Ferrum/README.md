# Ferrum implementation

Experimental LabAPI-hosted C# ↔ Rust framework, Windows x64 and Linux x64. Release metadata lives in `release.json`.

The repository overview is [../README.md](../README.md). Start with [installation](docs/installation.md), [configuration and localization](docs/localization.md), [updates](docs/updates.md), and [verification status](docs/verification.md).

Build only (does not run tests, installers or servers):

```powershell
./scripts/build.ps1 -Configuration Debug
./scripts/build.ps1 -Configuration Release
./scripts/publish-installer.ps1 -Platform win-x64
./scripts/publish-installer.ps1 -Platform linux-x64
```

Runtime requires LabAPI 1.1.7 and x64. This release does not load user Rust plugins or provide hot reload.
