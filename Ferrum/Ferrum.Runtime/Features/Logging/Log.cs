using System;
using Ferrum.Contracts;

namespace Ferrum.Runtime.Features.Logging;

public sealed class Log
{
    private readonly RuntimeContext context;
    private readonly ILocalizationService messages;
    public Log(RuntimeContext context, ILocalizationService messages) { this.context = context; this.messages = messages; }
    public void Raw(string message, ConsoleColor color = ConsoleColor.DarkYellow) => context.WriteConsole(message, color);
    public void Info(string key, params object[] args) => Write("info", key, ConsoleColor.Cyan, args);
    public void Warn(string key, Exception? error = null) { Write("warning", key, ConsoleColor.Yellow); Detail(error); }
    public void Error(string key, Exception? error = null) { Write("error", key, ConsoleColor.Red); Detail(error); }
    public void Debug(string key) { if (context.Config.Debug) Write("debug", key, ConsoleColor.Gray); }
    public void Detail(Exception? error) { if (context.Config.Debug && error != null) Write("debug", "{technical}", ConsoleColor.Gray, error.ToString()); }
    private void Write(string level, string key, ConsoleColor color, params object[] args)
    {
        string text = key == "{technical}" ? (string)args[0] : messages.GetMessage(key, args);
        context.WriteConsole("[" + messages.GetMessage("level." + level) + "] [Ferrum] " + text, color);
    }
}
