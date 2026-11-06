using System;
using System.Collections.Generic;
using System.IO;
using Ferrum.Interop.Platforms;

namespace Ferrum.Interop.Loading;

/// <summary>Roots successful OS handles until process exit; Dispose of a bridge never removes modules.</summary>
public static class NativeModuleRegistry
{
    private static readonly Dictionary<string, NativeModule> Modules = new(
        NativePlatform.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private static readonly object Gate = new();

    internal static NativeModule Open(string path)
    {
        _ = NativePlatform.Name;
        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Ferrum native library was not found.", fullPath);

        lock (Gate)
        {
            if (Modules.TryGetValue(fullPath, out NativeModule? existing))
                return existing;

            var module = new NativeModule(NativeLibraryLoader.Load(fullPath));
            Modules.Add(fullPath, module);
            return module;
        }
    }
}
