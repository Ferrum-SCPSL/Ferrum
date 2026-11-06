using System;

namespace Ferrum.Installer;

public sealed class InstallerException : Exception
{
    public string Key { get; }
    public object[] Arguments { get; }
    public InstallerException(string key, params object[] arguments) : base(key) { Key = key; Arguments = arguments; }
}
