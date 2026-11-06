# Architecture

LabAPI scans only the permanent `Ferrum.Loader.dll` plugin and stable `Ferrum.Contracts.dll` dependency. Loader has no assembly reference to Runtime, Interop, Localization or Updater. Its small localization and integrity helpers are compiled from shared sources with the `FERRUM_BOOTSTRAP` symbol into a separate bootstrap namespace, avoiding additional scanned dependencies.

Loader reads per-port YAML, resolves `FERRUM_HOME` → configured directory → `~/.config/Ferrum`, acquires the per-installation running lock, records a runtime attempt and validates the selected immutable runtime before loading any of its assemblies. Assembly resolution is restricted to the four known runtime DLL names. LabAPI and its server-provided YamlDotNet are compilation dependencies, never redistributed by Ferrum.

`IRuntime` and `RuntimeContext` in Contracts form protocol 1. Runtime owns the native bridge, diagnostics, localized banner and Updater. The only game-facing callbacks are console output, natural round-ending notification and restart scheduling. Bootstrap dispatches output to the captured game SynchronizationContext; when unavailable it queues output until a host event on the game thread. It never depends on current working directory or Assembly.Location for runtime discovery.

Successful readiness is acknowledged after the silent Rust probe and banner during PluginsEnabled. Native modules remain resident until process exit; Dispose prevents new bridge calls. No unload, hot reload or user Rust-plugin lifecycle is implemented.

`Runtime/<version>/<platform>` contains immutable Runtime, Interop, Localization, Updater, native code and its manifest. Configs, Logs, Plugins/Dependencies, Localizations and Updates are separate user-data directories. The public localization service lives behind interfaces in Contracts; implementation assemblies can change between complete process restarts.

Bootstrap and Contracts protocol changes require an explicit stopped-server installer upgrade. Such releases must increment their required protocol in release.json; compatible runtime-only releases keep protocol 1.
