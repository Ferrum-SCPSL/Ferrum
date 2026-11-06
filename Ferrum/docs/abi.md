# Experimental native ABI v2

Windows x64: x86_64-pc-windows-msvc. Linux x64: x86_64-unknown-linux-gnu. C ABI, Cdecl. Managed structs are sequential and Rust structures repr(C). FerrumBuffer has data at offset 0, byte length at offset 8 and total size 16 on x64.

Exports:

| Export | Purpose |
|---|---|
| ferrum_abi_version | Returns 2 before any other operation |
| ferrum_process_utf8 | Borrow pointer+byte length; synchronous callback, return owned echo |
| ferrum_set_locales_utf8 | Borrow log pointer+length and plugin pointer+length, copy both locale identifiers |
| ferrum_log_language_utf8 | Return the resolved log locale in an owned buffer |
| ferrum_plugin_language_utf8 | Return the resolved plugin locale in an owned buffer |
| ferrum_buffer_free | Free a Rust-owned output and reset its descriptor |

ConfigureLocales(string logLanguage, string pluginLanguage) accepts resolved en/ru/uk/zh-CN, not auto. The Rust locales module exposes log_language() and plugin_language() as owned Strings for future native integrations. Updating locale values is synchronized with an RwLock. It does not load plugins.

All borrowed inputs are valid only during synchronous calls. C# pins UTF-8 bytes and Rust never stores those addresses. Ordinary strings support Unicode, empty input and embedded NUL. Callback input is borrowed until callback returns and is copied by C#. Managed callback exceptions are caught, represented by callback-error status, and never cross the native boundary. Delegates remain rooted throughout the call.

All outputs, including diagnostics and locale getters, are Rust-owned pointer+length buffers. C# copies and frees in finally/Dispose with ferrum_buffer_free from the same module. Statuses: 0 success, 1 invalid argument, 2 invalid UTF-8, 3 callback error, 4 internal error, 5 contained panic. Rust uses panic=unwind and catch_unwind. Abort, allocation abort, memory corruption, and arbitrary invalid pointers are outside this recoverable contract.

Native diagnostic bytes remain technical data, not user-facing localization templates. Runtime logs use localized keys and only show technical exception details in Debug. The silent startup round-trip callback is never printed.

Modules remain resident until process exit. Test hooks and allocation/panic injection exports exist only with the test-hooks feature; production packaging never enables it.
