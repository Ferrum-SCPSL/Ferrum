using System;
using System.Collections.Generic;
using System.Globalization;

namespace Ferrum.Installer;

public sealed class InstallerOptions
{
    public string? ServerPath { get; set; }
    public string? LabApiPath { get; set; }
    public string? FerrumPath { get; set; }
    public int Port { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Channel { get; set; } = "stable";
    public string Language { get; set; } = "auto";
    public bool Rollback { get; set; }
    public bool ServerStopped { get; set; }
    public bool Debug { get; set; }
    public bool Help { get; set; }

    public static InstallerOptions Parse(string[] args)
    {
        var result = new InstallerOptions(); var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < args.Length; i++)
        {
            string option = args[i]; if (!seen.Add(option)) throw new ArgumentException(option);
            switch (option)
            {
                case "--help": result.Help = true; break;
                case "--rollback": result.Rollback = true; break;
                case "--server-stopped": result.ServerStopped = true; break;
                case "--debug": result.Debug = true; break;
                default:
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException(option);
                    string value = args[++i];
                    switch (option)
                    {
                        case "--server-path": result.ServerPath = value; break;
                        case "--labapi-path": result.LabApiPath = value; break;
                        case "--ferrum-path": result.FerrumPath = value; break;
                        case "--port": result.Port = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "--version": result.Version = value; break;
                        case "--channel": result.Channel = value; break;
                        case "--language": result.Language = value; break;
                        default: throw new ArgumentException(option);
                    }
                    break;
            }
        }
        if (result.Channel != "stable" && result.Channel != "stable_and_prerelease") throw new ArgumentException("--channel");
        if (Array.IndexOf(new[] { "auto", "en", "ru", "uk", "zh-CN" }, result.Language) < 0) throw new ArgumentException("--language");
        if (result.Port < 0 || result.Port > 65535) throw new ArgumentException("--port");
        return result;
    }
}
