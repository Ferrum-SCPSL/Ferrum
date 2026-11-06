using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Ferrum.Interop.Marshalling;

public static class Utf8Marshaller
{
    private static readonly UTF8Encoding Utf8 = new(false, true);

    internal static byte[] Encode(string value) => Utf8.GetBytes(value);

    internal static string Decode(IntPtr data, UIntPtr length)
    {
        ulong count = length.ToUInt64();
        if (count > int.MaxValue || (data == IntPtr.Zero && count != 0))
            throw new InvalidDataException("Invalid native UTF-8 buffer.");
        if (count == 0)
            return string.Empty;

        byte[] copy = new byte[(int)count];
        Marshal.Copy(data, copy, 0, copy.Length);
        return Utf8.GetString(copy);
    }
}
