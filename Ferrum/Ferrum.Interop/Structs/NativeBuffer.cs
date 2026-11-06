using System;
using System.Runtime.InteropServices;

namespace Ferrum.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct NativeBuffer
{
    public IntPtr Data;
    public UIntPtr Length;
}
