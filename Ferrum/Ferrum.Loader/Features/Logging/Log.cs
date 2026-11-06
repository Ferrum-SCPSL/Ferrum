using System;
using Ferrum.Loader.Enums;

namespace Ferrum.Loader.Features.Logging;

/// <summary>Ferrum-owned formatting and direct server-console output, independent of LabAPI's logger.</summary>
public static class Log
{
    public static void Raw(string message, ConsoleColor color = ConsoleColor.Cyan) => ServerConsoleWriter.Write(message, color);

    internal static void Debug(string message) => Write(message, LogLevel.Debug, "Ferrum");

    internal static void Info(string message) => Write(message, LogLevel.Info, "Ferrum");

    internal static void Warn(string message) => Write(message, LogLevel.Warning, "Ferrum");

    internal static void Error(string message) => Write(message, LogLevel.Error, "Ferrum");

    internal static void Rust(string message) => Write(message, LogLevel.Info, "Ferrum/Rust");

    private static void Write(string message, LogLevel level, string source)
    {
        string label = level == LogLevel.Warning ? "WARN" : level.ToString().ToUpperInvariant();
        ConsoleColor color = level switch
        {
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            _ => ConsoleColor.Cyan,
        };

        ServerConsoleWriter.Write($"[{label}] [{source}] {message}", color);
    }
}
