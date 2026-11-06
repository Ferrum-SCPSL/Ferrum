using System.Runtime.InteropServices;

namespace Ferrum.Interop;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate uint AbiVersion();
