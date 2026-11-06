using System;
using System.Runtime.InteropServices;

namespace Ferrum.Interop;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate NativeStatus ProcessUtf8(IntPtr data, UIntPtr length, LogCallback? callback, out NativeBuffer output);
