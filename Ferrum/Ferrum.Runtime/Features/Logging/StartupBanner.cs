using System;
using System.Collections.Generic;
using Ferrum.Contracts;
using Ferrum.Runtime.Features.Metadata;

namespace Ferrum.Runtime.Features.Logging;

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

    public static void Show(Log log, ILocalizationService messages, string version, string branch)
    {
        DateTime date = DateTime.Today;
        var lines = new List<string>
        {
            Logo.Replace("\n", Environment.NewLine),
            string.Empty,
        };

        string? greeting = HolidayGreeting.GetMessage(date, messages);
        if (greeting != null)
            lines.Add(greeting);

        lines.AddRange(new[]
        {
            messages.GetMessage("banner.thanks"),
            messages.GetMessage("banner.version", branch, version),
            messages.GetMessage("banner.contributors", string.Join(", ", Contributors.Names)),
        });

        string message = string.Join(Environment.NewLine, lines);
        log.Raw(message, ConsoleColor.DarkYellow);
    }
}
