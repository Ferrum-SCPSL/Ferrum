# Configuration and localization

Main config: `<actual LabAPI>/configs/<port>/Ferrum/config.yml`, resolved using PathManager. Missing config is created with English comments and default values. Existing YAML is read without saving; unknown keys remain untouched. Invalid YAML/settings disable Ferrum and are not overwritten. Configuration changes take effect after a complete server restart.

```yaml
is_enabled: true
debug: false
ferrum_directory_path: ''
log_language: auto
plugin_language: auto
update_channel: stable
enable_auto_updates: true
update_check_interval_minutes: 360
restart_after_update: true
```

`is_enabled: false` prevents runtime/native initialization and update checks. `debug` controls technical exception details and Debug-level messages. Both language settings independently accept auto, en, ru, uk, zh-CN. Auto uses the server process UI culture; if undefined, LC_ALL, LC_MESSAGES and LANG are considered. Unsupported cultures use English; simplified Chinese variants map to zh-CN, traditional Chinese does not silently select simplified text. The update interval must be between 1 and 10080 minutes. FERRUM_HOME overrides ferrum_directory_path; an empty path selects the server account's `~/.config/Ferrum`.

Four UTF-8 `key,value` CSV catalogs are embedded and seeded into `.config/Ferrum/Localizations` when absent. CSV supports commas, doubled quotes and multiline fields. Existing overrides are never overwritten. Resolution: external selected language → embedded selected language → external English → embedded English → message key. Malformed catalogs and invalid format strings fall back; native/OS exception details are only emitted with Debug enabled.

The framework banner, holidays, levels, bootstrap, updater and installer messages have all four translations. Ferrum's log sink calls ServerConsole directly, without LabAPI Logger. Banner color remains DarkYellow, version has a v prefix, and contributor names come from CONTRIBUTORS.txt. Holiday greeting uses one local server date: October 25–31 inclusive for Halloween, December 25–January 7 inclusive for New Year, absent otherwise.

## Plugin contract

Reference Ferrum.Contracts and retrieve `FerrumServices.Localization` after Ferrum initialization. This task does not include a Rust plugin loader.

```csharp
ILocalizationService service = FerrumServices.Localization
    ?? throw new InvalidOperationException("Ferrum is not initialized.");
ILocalizer messages = service.RegisterPlugin("Example", "/absolute/Ferrum/Localizations/Plugins/Example");
string text = messages.GetMessage("example.started", "Example");
```

Plugin catalogs use the same four language filenames, keys and numeric .NET format placeholders. They inherit plugin_language, fall back to the plugin's English catalog and finally the key. Plugins decide whether to support this API; Ferrum does not translate arbitrary third-party text. Rust core receives both resolved locale identifiers at startup and exposes owned UTF-8 getters plus public Rust locale accessors for future plugin integration. This is a language contract, not a plugin SDK or gameplay event API.
