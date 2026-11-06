using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Ferrum.Contracts;

public static class JsonFile
{
    public static T Read<T>(string path) => Parse<T>(File.ReadAllText(path, Encoding.UTF8));
    public static T Parse<T>(string text)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        return (T)(new DataContractJsonSerializer(typeof(T)).ReadObject(stream)
            ?? throw new InvalidDataException("Empty JSON document."));
    }

    public static void WriteAtomic<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                new DataContractJsonSerializer(typeof(T)).WriteObject(stream, value);
                stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
