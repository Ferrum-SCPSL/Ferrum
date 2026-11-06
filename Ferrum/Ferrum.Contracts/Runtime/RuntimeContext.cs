using System;

namespace Ferrum.Contracts;

public sealed class RuntimeContext
{
    public FerrumConfig Config { get; set; } = new();
    public string Root { get; set; } = string.Empty;
    public string RuntimeDirectory { get; set; } = string.Empty;
    public string StateDirectory { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public Action<string, ConsoleColor> WriteConsole { get; set; } = (_, _) => { };
    public Func<bool> ScheduleRestart { get; set; } = () => false;
    public Action MarkReady { get; set; } = () => { };
}
