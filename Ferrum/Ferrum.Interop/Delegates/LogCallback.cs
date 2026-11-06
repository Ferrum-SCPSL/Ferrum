using System;
using System.Runtime.InteropServices;

namespace Ferrum.Interop;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int LogCallback(IntPtr data, UIntPtr length);
