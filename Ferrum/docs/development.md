# Development

SDK-style managed runtime projects target net48, C# 12, x64 and nullable. Shared Contracts, Localization and Updater target netstandard2.0 for use in both the game runtime and the net10.0 installer. Contracts is protocol 1. Rust is pinned to 1.99.1; .NET SDK to 10.0.400. Cargo.lock is committed; Cargo builds use --locked.

Windows needs MSVC x64 and a Windows SDK. common.ps1 discovers vcvars64 using vswhere or FERRUM_VS_INSTALL, and imports its environment only into the build process. Linux native builds must run on Linux with the pinned toolchain and normal GNU linker toolchain. Framework reference assemblies are restored from NuGet, so a Mono runtime is not required to compile the managed projects on Linux.

From Ferrum/:

```powershell
./scripts/build.ps1 -Configuration Debug
./scripts/build.ps1 -Configuration Release
./scripts/publish-installer.ps1 -Platform win-x64
./scripts/publish-installer.ps1 -Platform linux-x64
```

Builds package only production native code unless -TestHooks is explicitly supplied. They never execute tests or installers. package.py inspects PE/ELF architecture, CSV key/placeholder consistency, archive membership and content hashes without loading Ferrum. Product version, branch, pre-release status and protocol requirements come from release.json; build.ps1 passes its version to MSBuild.

Ordinary CI retains the owner's Debug-only matrix on Windows/Linux. Release workflow builds Release on both platforms, publishes self-contained installers, assembles six complete assets and publishes the GitHub release only after both jobs succeed. Workflow permissions are read-only except the publish job's contents:write. No workflow runs tests, smoke programs, installers or a dedicated server.

The owner removed managed smoke-test sources during implementation; the removal is preserved and the solution no longer references that project. Historical test/server smoke scripts are not part of build/release execution and predate the versioned installer layout. Rust unit-test source remains, but is not executed under the current validation policy.
