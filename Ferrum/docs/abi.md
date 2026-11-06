# Ferrum ABI v1 (experimental)

The ABI is a PoC contract, not the stable Ferrum plugin SDK. Both sides are x64.
Windows uses MSVC; Linux uses the GNU/glibc target and `libdl.so.2`.

```c
#include <stddef.h>
#include <stdint.h>

typedef struct { uint8_t *data; size_t len; } FerrumBuffer;
typedef int32_t (*FerrumLog)(const uint8_t *data, size_t len);

uint32_t ferrum_abi_version(void); /* 1 */
int32_t ferrum_process_utf8(const uint8_t *data, size_t len,
                          FerrumLog log, FerrumBuffer *output);
int32_t ferrum_buffer_free(FerrumBuffer *buffer);
```

All functions/callbacks use C calling convention (`Cdecl`). The buffer is 16 bytes:
pointer at offset 0, length at offset 8. Status is signed 32-bit:
0=Ok, 1=InvalidArgument, 2=InvalidUtf8, 3=CallbackError, 4=InternalError, 5=Panic.
`InternalError` is reserved for native operations and used by managed load diagnostics.

## Ownership and lifetimes

- Input is borrowed for the synchronous call. A null pointer is valid only for
  zero-length input; nonzero input must point to readable memory, length <= `isize::MAX`.
- Input must contain strict UTF-8; embedded zero bytes are ordinary data.
- `output` must be nonnull, aligned, writable, and not alias input. It must not
  already contain an unreleased buffer. A nonnull output is initialized before work.
- On success the output is a Rust-owned copy of the input. On recoverable errors
  it is an owned UTF-8 diagnostic. Empty output is `{NULL,0}` with no allocation.
- The caller must release every output, including errors, with this library's
  `ferrum_buffer_free`. The implementation uses `Box<[u8]>`, never C# allocation.
- Free resets the original descriptor to `{NULL,0}`. Freeing that empty descriptor
  again succeeds. Freeing a copied/stale descriptor, changing its length or pointer,
  or passing another allocator's buffer violates the contract and may corrupt memory.
- The C# wrapper copies and strictly decodes the result before freeing it in `finally`.
  A result-decoding exception still releases the original allocation.
- Callback gets borrowed UTF-8 bytes, valid only until it returns. It runs once
  synchronously for valid input on the original caller thread; Rust does not save it.
  C# roots the delegate and catches its exceptions, returning nonzero to Rust.
- `RoundTrip`/`Dispose` are serialized. Dispose rejects new calls and waits for an
  active call on another thread; native modules stay loaded for process lifetime.
  No native worker threads, global logger registration or physical unload exist.

Null output returns InvalidArgument without a diagnostic. Null callback and invalid
UTF-8/input combinations return owned error diagnostics when output is valid.
Nonempty arbitrary invalid pointers, access violations, allocator failure, aborting
panic and process termination are not recoverable. Rust unwinding panics raised by
the implementation are caught before the C boundary (`panic=unwind`).

## Test-only exports

`--features test-hooks` adds `ferrum_test_outstanding_allocations` and
`ferrum_test_panic`. Production packages are built in a different target directory
without that feature. Test fixtures deliberately omit exports or report ABI 999.

Sources: [Rust FFI](https://doc.rust-lang.org/nomicon/ffi.html),
[catch_unwind](https://doc.rust-lang.org/std/panic/fn.catch_unwind.html),
[Mono P/Invoke](https://www.mono-project.com/docs/advanced/pinvoke/).
