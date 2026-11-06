//! Experimental Ferrum C ABI v1. See docs/abi.md for pointer and ownership contracts.
#![deny(unsafe_op_in_unsafe_fn)]

use std::panic::{AssertUnwindSafe, catch_unwind};
use std::{ptr, slice, str};

#[cfg(feature = "test-hooks")]
use std::sync::atomic::{AtomicUsize, Ordering};

#[cfg(feature = "test-hooks")]
static OUTSTANDING: AtomicUsize = AtomicUsize::new(0);

#[repr(i32)]
#[derive(Debug, PartialEq, Eq, Clone, Copy)]
pub enum Status {
    Ok = 0,
    InvalidArgument = 1,
    InvalidUtf8 = 2,
    CallbackError = 3,
    InternalError = 4,
    Panic = 5,
}

#[repr(C)]
pub struct FerrumBuffer {
    pub data: *mut u8,
    pub len: usize,
}

impl FerrumBuffer {
    fn empty() -> Self {
        Self {
            data: ptr::null_mut(),
            len: 0,
        }
    }

    fn owned(bytes: Vec<u8>) -> Self {
        if bytes.is_empty() {
            return Self::empty();
        }
        let mut bytes = bytes.into_boxed_slice();
        let result = Self {
            data: bytes.as_mut_ptr(),
            len: bytes.len(),
        };
        std::mem::forget(bytes);
        #[cfg(feature = "test-hooks")]
        OUTSTANDING.fetch_add(1, Ordering::SeqCst);
        result
    }
}

pub type LogCallback = unsafe extern "C" fn(*const u8, usize) -> i32;
type Outcome = Result<Vec<u8>, (Status, &'static str)>;

/// # Safety
/// `output` must be null or valid, aligned, writable storage for an empty buffer.
unsafe fn guarded(output: *mut FerrumBuffer, operation: impl FnOnce() -> Outcome) -> Status {
    if output.is_null() {
        return Status::InvalidArgument;
    }
    // SAFETY: the caller supplies writable storage and no outstanding owned buffer.
    unsafe { output.write(FerrumBuffer::empty()) };
    let result = catch_unwind(AssertUnwindSafe(|| {
        let (status, bytes) = match operation() {
            Ok(bytes) => (Status::Ok, bytes),
            Err((status, message)) => (status, message.as_bytes().to_vec()),
        };
        // SAFETY: output was checked above; ownership transfers only after allocation succeeds.
        unsafe { output.write(FerrumBuffer::owned(bytes)) };
        status
    }));
    match result {
        Ok(status) => status,
        Err(payload) => {
            // Our implementation only raises string panics. Do not execute an arbitrary
            // panic payload destructor on the C boundary, where a second panic would abort.
            if payload.is::<&str>() || payload.is::<String>() {
                drop(payload);
            } else {
                std::mem::forget(payload);
            }
            // Allocator failure/abort is explicitly outside the recoverable contract.
            unsafe { output.write(FerrumBuffer::owned(b"Rust panic contained.".to_vec())) };
            Status::Panic
        }
    }
}

#[unsafe(no_mangle)]
pub extern "C" fn ferrum_abi_version() -> u32 {
    1
}

/// Echo valid UTF-8 and synchronously invoke the host logger once.
///
/// # Safety
/// Nonempty input must be readable for `len` bytes, at most `isize::MAX`.
/// `output` must be writable, aligned and not alias input or another live buffer.
/// Callback pointers must be valid for this call and must not unwind.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn ferrum_process_utf8(
    data: *const u8,
    len: usize,
    callback: Option<LogCallback>,
    output: *mut FerrumBuffer,
) -> Status {
    // SAFETY: delegated contracts are exactly those documented on this export.
    unsafe {
        guarded(output, || {
            if len > isize::MAX as usize || (data.is_null() && len != 0) {
                return Err((Status::InvalidArgument, "Invalid input pointer or length."));
            }
            let callback = callback.ok_or((Status::InvalidArgument, "Missing log callback."))?;
            let bytes = if len == 0 {
                &[][..]
            } else {
                slice::from_raw_parts(data, len)
            };
            let text = str::from_utf8(bytes)
                .map_err(|_| (Status::InvalidUtf8, "Input is not valid UTF-8."))?;
            let message = format!("Received UTF-8: {text}");
            if callback(message.as_ptr(), message.len()) != 0 {
                return Err((Status::CallbackError, "Host logging callback failed."));
            }
            Ok(bytes.to_vec())
        })
    }
}

/// Release one Rust-owned buffer and reset its descriptor. Empty buffers are accepted.
///
/// # Safety
/// Buffer must point to writable storage. Its data/len must be unmodified values
/// returned by this library, with ownership not previously released or copied.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn ferrum_buffer_free(buffer: *mut FerrumBuffer) -> Status {
    if buffer.is_null() {
        return Status::InvalidArgument;
    }
    // SAFETY: descriptor validity and exclusive ownership are caller obligations.
    let value = unsafe { &mut *buffer };
    if value.data.is_null() {
        return if value.len == 0 {
            Status::Ok
        } else {
            Status::InvalidArgument
        };
    }
    if value.len == 0 || value.len > isize::MAX as usize {
        return Status::InvalidArgument;
    }
    let owned = ptr::slice_from_raw_parts_mut(value.data, value.len);
    *value = FerrumBuffer::empty();
    // SAFETY: owned came from Box<[u8]> in FerrumBuffer::owned, with its original length.
    unsafe { drop(Box::from_raw(owned)) };
    #[cfg(feature = "test-hooks")]
    OUTSTANDING.fetch_sub(1, Ordering::SeqCst);
    Status::Ok
}

#[cfg(feature = "test-hooks")]
#[unsafe(no_mangle)]
pub extern "C" fn ferrum_test_outstanding_allocations() -> usize {
    OUTSTANDING.load(Ordering::SeqCst)
}

/// # Safety
/// Same output contract as ferrum_process_utf8. Present only in test builds.
#[cfg(feature = "test-hooks")]
#[unsafe(no_mangle)]
pub unsafe extern "C" fn ferrum_test_panic(output: *mut FerrumBuffer) -> Status {
    // SAFETY: the test host provides a valid output descriptor.
    unsafe { guarded(output, || panic!("controlled Ferrum smoke-test panic")) }
}

#[cfg(test)]
mod tests {
    use super::*;

    unsafe extern "C" fn logger(_: *const u8, _: usize) -> i32 {
        0
    }

    #[test]
    fn layout_and_status_match_abi_v1() {
        assert_eq!(ferrum_abi_version(), 1);
        assert_eq!(std::mem::size_of::<FerrumBuffer>(), 16);
        assert_eq!(std::mem::offset_of!(FerrumBuffer, len), 8);
        assert_eq!(std::mem::size_of::<Status>(), 4);
    }

    #[test]
    fn unicode_and_empty_buffers_have_explicit_ownership() {
        for text in ["", "Hello", "Привет 🦀", "before\0after"] {
            let mut output = FerrumBuffer::empty();
            // SAFETY: input is a live Rust str, output writable, callback synchronous.
            unsafe {
                assert_eq!(
                    ferrum_process_utf8(text.as_ptr(), text.len(), Some(logger), &mut output),
                    Status::Ok
                );
                if !text.is_empty() {
                    assert_eq!(
                        slice::from_raw_parts(output.data, output.len),
                        text.as_bytes()
                    );
                }
                assert_eq!(ferrum_buffer_free(&mut output), Status::Ok);
                assert!(output.data.is_null());
                assert_eq!(output.len, 0);
                assert_eq!(ferrum_buffer_free(&mut output), Status::Ok);
            }
        }
    }

    #[test]
    fn invalid_arguments_and_utf8_return_owned_diagnostics() {
        let mut output = FerrumBuffer::empty();
        unsafe {
            assert_eq!(
                ferrum_process_utf8(ptr::null(), 1, Some(logger), &mut output),
                Status::InvalidArgument
            );
            assert_eq!(ferrum_buffer_free(&mut output), Status::Ok);
            assert_eq!(
                ferrum_process_utf8([0xff].as_ptr(), 1, Some(logger), &mut output),
                Status::InvalidUtf8
            );
            assert_eq!(ferrum_buffer_free(&mut output), Status::Ok);
            assert_eq!(
                ferrum_process_utf8(ptr::null(), 0, Some(logger), ptr::null_mut()),
                Status::InvalidArgument
            );
            assert_eq!(ferrum_buffer_free(ptr::null_mut()), Status::InvalidArgument);
        }
    }
}
