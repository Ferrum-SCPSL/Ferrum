using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Ferrum.Updater;

/// <summary>Accepts regular files only; rejects links, duplicates, traversal and oversized content.</summary>
public static class SafeArchive
{
    private const long Limit = 128 * 1024 * 1024;
    public static void Extract(string archive, string destination, bool gzipTar)
    {
        Directory.CreateDirectory(destination);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long size = 0;
        if (!gzipTar)
        {
            ZipMetadata.Validate(archive);
            using var zip = ZipFile.OpenRead(archive);
            foreach (var entry in zip.Entries)
            {
                if (entry.FullName.EndsWith("/", StringComparison.Ordinal)) continue;
                string target = Target(destination, entry.FullName, seen);
                size = checked(size + entry.Length); if (size > Limit) throw new InvalidDataException("Archive too large.");
                using Stream source = entry.Open(); using Stream output = File.Create(target); CopyExact(source, output, entry.Length);
            }
            return;
        }
        using var input = File.OpenRead(archive); using var tar = new GZipStream(input, CompressionMode.Decompress);
        byte[] header = new byte[512];
        while (ReadBlock(tar, header))
        {
            if (header.All(value => value == 0)) break;
            long checksum = Octal(header, 148, 8); long sum = 0;
            for (int i = 0; i < 512; i++) sum += i >= 148 && i < 156 ? 32 : header[i];
            if (sum != checksum) throw new InvalidDataException("Invalid TAR checksum.");
            string name = Text(header, 0, 100), prefix = Text(header, 345, 155);
            if (prefix.Length != 0) name = prefix + "/" + name;
            long length = Octal(header, 124, 12);
            if (header[156] == (byte)'5' && length == 0) continue;
            if (header[156] != 0 && header[156] != (byte)'0') throw new InvalidDataException("Unsupported TAR entry.");
            size = checked(size + length); if (length < 0 || size > Limit) throw new InvalidDataException("Archive too large.");
            string target = Target(destination, name, seen);
            using (Stream output = File.Create(target)) CopyExact(tar, output, length);
            long padding = (512 - length % 512) % 512;
            CopyExact(tar, Stream.Null, padding);
        }
    }

    private static string Target(string directory, string name, HashSet<string> seen)
    {
        if (!seen.Add(name) || seen.Count > 64) throw new InvalidDataException("Duplicate or excessive entries.");
        string target = PackageIntegrity.ResolveEntry(directory, name);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!); return target;
    }
    private static string Text(byte[] bytes, int offset, int count) => Encoding.ASCII.GetString(bytes, offset, count).TrimEnd('\0');
    private static long Octal(byte[] bytes, int offset, int count) => Convert.ToInt64(Text(bytes, offset, count).Trim(' ', '\0'), 8);
    private static bool ReadBlock(Stream input, byte[] block)
    {
        int offset = 0;
        while (offset < block.Length) { int read = input.Read(block, offset, block.Length - offset); if (read == 0) { if (offset == 0) return false; throw new EndOfStreamException(); } offset += read; }
        return true;
    }
    private static void CopyExact(Stream input, Stream output, long length)
    {
        byte[] buffer = new byte[81920];
        while (length > 0) { int read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, length)); if (read == 0) throw new EndOfStreamException(); output.Write(buffer, 0, read); length -= read; }
    }
}
