using System;
using System.IO;

namespace Ferrum.Updater;

public static class ZipMetadata
{
    public static void Validate(string path)
    {
        using var stream = File.OpenRead(path); using var reader = new BinaryReader(stream);
        int length = (int)Math.Min(65557, stream.Length);
        stream.Position = stream.Length - length; byte[] tail = reader.ReadBytes(length); int end = -1;
        for (int i = tail.Length - 22; i >= 0; i--)
            if (BitConverter.ToUInt32(tail, i) == 0x06054b50 && i + 22 + BitConverter.ToUInt16(tail, i + 20) == tail.Length) { end = i; break; }
        if (end < 0 || BitConverter.ToUInt16(tail, end + 4) != 0 || BitConverter.ToUInt16(tail, end + 6) != 0) throw new InvalidDataException("Unsupported ZIP directory.");
        int count = BitConverter.ToUInt16(tail, end + 10); if (count > 64) throw new InvalidDataException("Too many ZIP entries.");
        stream.Position = BitConverter.ToUInt32(tail, end + 16);
        for (int i = 0; i < count; i++)
        {
            byte[] header = reader.ReadBytes(46);
            if (header.Length != 46 || BitConverter.ToUInt32(header, 0) != 0x02014b50) throw new InvalidDataException("Invalid ZIP directory.");
            uint attributes = BitConverter.ToUInt32(header, 38); uint kind = (attributes >> 16) & 0xF000;
            if (kind != 0 && kind != 0x8000 && kind != 0x4000) throw new InvalidDataException("ZIP links are not supported.");
            stream.Seek(BitConverter.ToUInt16(header, 28) + BitConverter.ToUInt16(header, 30) + BitConverter.ToUInt16(header, 32), SeekOrigin.Current);
        }
    }
}
