using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Ferrum.Loader.Features;

/// <summary>Only resolve the allowlisted payload assemblies from one immutable version.</summary>
public sealed class RuntimeResolver : IDisposable
{
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    { "Ferrum.Runtime", "Ferrum.Interop", "Ferrum.Localization", "Ferrum.Updater" };
    private readonly string directory;
    public RuntimeResolver(string directory) { this.directory = directory; AppDomain.CurrentDomain.AssemblyResolve += Resolve; }
    private Assembly? Resolve(object? sender, ResolveEventArgs args)
    {
        string name = new AssemblyName(args.Name).Name ?? string.Empty;
        if (!Allowed.Contains(name)) return null;
        foreach (Assembly existing in AppDomain.CurrentDomain.GetAssemblies())
            if (existing.GetName().Name == name) return existing;
        return Assembly.Load(File.ReadAllBytes(Path.Combine(directory, name + ".dll")));
    }
    public Assembly LoadRuntime() => Assembly.Load(File.ReadAllBytes(Path.Combine(directory, "Ferrum.Runtime.dll")));
    public void Dispose() => AppDomain.CurrentDomain.AssemblyResolve -= Resolve;
}
