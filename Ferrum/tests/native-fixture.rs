// Deliberately incomplete ABI libraries; never distributed in the runtime package.
#[unsafe(no_mangle)]
pub extern "C" fn ferrum_abi_version() -> u32 {
    if cfg!(incompatible_abi) { 999 } else { 2 }
}
