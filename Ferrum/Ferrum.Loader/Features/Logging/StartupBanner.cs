using System;
using System.Collections.Generic;
using Ferrum.Loader.Features.Metadata;

namespace Ferrum.Loader.Features.Logging;

/// <summary>A single welcome message emitted after the host finishes enabling plugins.</summary>
public static class StartupBanner
{
    private const string Logo =
        "███████╗███████╗██████╗ ██████╗ ██╗   ██╗███╗   ███╗\n" +
        "██╔════╝██╔════╝██╔══██╗██╔══██╗██║   ██║████╗ ████║\n" +
        "█████╗  █████╗  ██████╔╝██████╔╝██║   ██║██╔████╔██║\n" +
        "██╔══╝  ██╔══╝  ██╔══██╗██╔══██╗██║   ██║██║╚██╔╝██║\n" +
        "██║     ███████╗██║  ██║██║  ██║╚██████╔╝██║ ╚═╝ ██║\n" +
        "╚═╝     ╚══════╝╚═╝  ╚═╝╚═╝  ╚═╝ ╚═════╝ ╚═╝     ╚═╝";

    public static void Show()
    {
        DateTime date = DateTime.Today;
        var lines = new List<string>
        {
            Logo.Replace("\n", Environment.NewLine),
            string.Empty,
        };

        string? greeting = HolidayGreeting.GetMessage(date);
        if (greeting != null)
            lines.Add(greeting);

        lines.AddRange(new[]
        {
            "Thank you for using and supporting the Ferrum framework!",
            $"Branch: {ReleaseInfo.Branch} | Version: v{ReleaseInfo.Version}",
            $"Contributors: {string.Join(", ", Contributors.Names)}",
        });

        string message = string.Join(Environment.NewLine, lines);
        Log.Raw(message, ConsoleColor.DarkYellow);
    }
}
