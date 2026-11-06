//! Owned locale identifiers shared with future native plugins. No plugin loader is provided here.
use super::{FerrumBuffer, Status, guarded};
use std::sync::{OnceLock, RwLock};
use std::{slice, str};

struct Locales {
    log: String,
    plugin: String,
}

fn locales() -> &'static RwLock<Locales> {
    static LOCALES: OnceLock<RwLock<Locales>> = OnceLock::new();
    LOCALES.get_or_init(|| {
        RwLock::new(Locales {
            log: "en".into(),
            plugin: "en".into(),
        })
    })
}

/// The resolved locale which future plugins should use unless they override it.
pub fn plugin_language() -> String {
    locales()
        .read()
        .unwrap_or_else(|poison| poison.into_inner())
        .plugin
        .clone()
}

pub fn log_language() -> String {
    locales()
        .read()
        .unwrap_or_else(|poison| poison.into_inner())
        .log
        .clone()
}

unsafe fn read_locale(data: *const u8, len: usize) -> Result<String, (Status, &'static str)> {
    if data.is_null() || len == 0 || len > 16 {
        return Err((Status::InvalidArgument, "Invalid locale pointer or length."));
    }
    // SAFETY: caller guarantees readable bytes for this synchronous operation.
    let text = str::from_utf8(unsafe { slice::from_raw_parts(data, len) })
        .map_err(|_| (Status::InvalidUtf8, "Invalid locale UTF-8."))?;
    if !matches!(text, "en" | "ru" | "uk" | "zh-CN") {
        return Err((Status::InvalidArgument, "Unsupported locale."));
    }
    Ok(text.to_owned())
}

/// Configure owned locale identifiers. All input bytes are borrowed only until return.
///
/// # Safety
/// Nonempty pointers must be readable for their lengths. Output follows the process_utf8 contract.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn ferrum_set_locales_utf8(
    log_data: *const u8,
    log_len: usize,
    plugin_data: *const u8,
    plugin_len: usize,
    output: *mut FerrumBuffer,
) -> Status {
    // SAFETY: pointer contracts are delegated unchanged to the guarded implementation.
    unsafe {
        guarded(output, || {
            let log = read_locale(log_data, log_len)?;
            let plugin = read_locale(plugin_data, plugin_len)?;
            let mut value = locales()
                .write()
                .unwrap_or_else(|poison| poison.into_inner());
            *value = Locales { log, plugin };
            Ok(Vec::new())
        })
    }
}

/// Return the plugin language as a Rust-owned UTF-8 buffer; release using ferrum_buffer_free.
///
/// # Safety
/// Output must follow the process_utf8 output contract.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn ferrum_plugin_language_utf8(output: *mut FerrumBuffer) -> Status {
    // SAFETY: the host supplies the same writable output descriptor as other ABI operations.
    unsafe { guarded(output, || Ok(plugin_language().into_bytes())) }
}

/// Return the framework log language as a Rust-owned UTF-8 buffer.
///
/// # Safety
/// Output must follow the process_utf8 output contract.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn ferrum_log_language_utf8(output: *mut FerrumBuffer) -> Status {
    // SAFETY: the host supplies the same writable output descriptor as other ABI operations.
    unsafe { guarded(output, || Ok(log_language().into_bytes())) }
}
