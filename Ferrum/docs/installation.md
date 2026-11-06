# Installation / Установка

Install into an existing SCP:SL x64 dedicated server with LabAPI 1.1.7. Stop the server and its LocalAdmin process first. The installer does not install SteamCMD, SCP:SL or OS dependencies and never starts a server.

Download the installer for the platform from the same Ferrum release:

- Windows: `Ferrum.Installer-Win.exe`
- Linux: `Ferrum.Installer-Linux` (`chmod +x Ferrum.Installer-Linux` before use)

Run without arguments for interactive path, port and stopped-server confirmation. Non-interactive examples:

```powershell
./Ferrum.Installer-Win.exe --server-path 'D:\SteamLibrary\steamapps\common\SCP Secret Laboratory Dedicated Server' --port 7777 --server-stopped
```

```sh
./Ferrum.Installer-Linux --server-path /home/container --labapi-path '/home/container/.config/SCP Secret Laboratory/LabAPI' --ferrum-path /home/container/.config/Ferrum --port 7777 --server-stopped
```

Use `--help` for all options. `--version v0.1.0` explicitly selects that release, including its pre-release status. The default is the installer's own release version. `--version latest --channel stable` selects the newest stable release; `--channel stable_and_prerelease` also permits pre-releases. The channel option controls that installation request, and does not overwrite an existing runtime config. `--language` sets installer output only; runtime languages are configured in YAML.

Default LabAPI paths follow the OS ApplicationData directory and server `hoster_policy.txt` (`gamedir_for_configs: true` selects the server's AppData folder). Explicit --labapi-path handles host-specific layouts. Ferrum data defaults to the profile of the account running the installer, in `.config/Ferrum` on both Windows and Linux. Run as the server account or supply paths explicitly. FERRUM_HOME has highest priority. An existing config must be explicitly edited before changing its configured root; the installer does not silently rewrite it.

The installer verifies checksums and manifests, places runtime in `Runtime/<version>/<platform>`, installs Loader into LabAPI/plugins/global and Contracts into LabAPI/dependencies/global, and seeds missing CSV files. It preserves existing configs and localization overrides. Known legacy Interop/native binaries are backed up before removal. Replacement backups remain in the installation's Updates directory. `--rollback` selects the previous validated runtime for the chosen port with the server stopped.

Runtime archives contain `bootstrap/`, `runtime/`, `localizations/` and manifest.json. They are installer payloads, not archives to unpack directly into the old server layout. No test binaries, symbols, LabAPI or proprietary game DLLs are included. The Windows Rust library uses the MSVC runtime provided by the server's supported Windows environment; installation does not download redistributables.

Installation, rollback and server runtime behavior have not been exercised in this release: only builds and static artifact inspection were authorized. Linux dedicated-server compatibility remains unconfirmed.
