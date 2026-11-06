using System;
using System.Runtime.InteropServices;

namespace Ferrum.Interop;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate NativeStatus SetLocalesUtf8(IntPtr logData, UIntPtr logLength, IntPtr pluginData, UIntPtr pluginLength, out NativeBuffer output);
