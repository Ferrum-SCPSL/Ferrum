using System;
using System.IO;

namespace Ferrum.Contracts;

public sealed class RuntimeStateStore
{
    public string DirectoryPath { get; }
    private string FilePath => Path.Combine(DirectoryPath, "runtime.json");
    public RuntimeStateStore(string directory) => DirectoryPath = directory;

    public RuntimeState Read()
    {
        using var gate = new InstallationLock(DirectoryPath, "state.lock");
        return File.Exists(FilePath) ? JsonFile.Read<RuntimeState>(FilePath) : new RuntimeState();
    }

    public RuntimeState Change(Action<RuntimeState> change)
    {
        using var gate = new InstallationLock(DirectoryPath, "state.lock");
        RuntimeState state = File.Exists(FilePath) ? JsonFile.Read<RuntimeState>(FilePath) : new RuntimeState();
        change(state);
        JsonFile.WriteAtomic(FilePath, state);
        return state;
    }
}
