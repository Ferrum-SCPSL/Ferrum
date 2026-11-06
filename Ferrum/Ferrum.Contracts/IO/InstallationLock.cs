using System;
using System.IO;

namespace Ferrum.Contracts;

/// <summary>Cross-process lock. Never delete lock files while another process may hold one.</summary>
public sealed class InstallationLock : IDisposable
{
    private readonly FileStream stream;
    public InstallationLock(string directory, string name = "install.lock")
    {
        Directory.CreateDirectory(directory);
        stream = new FileStream(Path.Combine(directory, name), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }
    public void Dispose() => stream.Dispose();
}
