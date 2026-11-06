using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Ferrum.Contracts;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Ferrum.Installer;

public sealed class ServerInstallation
{
    public string ServerPath { get; }
    public string LabApiPath { get; }
    public string FerrumPath { get; }
    public string ConfigPath { get; }
    public string StateDirectory { get; }
    public int Port { get; }
    public ServerInstallation(InstallerOptions options)
    {
        ServerPath = Path.GetFullPath(options.ServerPath ?? throw new ArgumentException("--server-path"));
        Port = options.Port; if (Port < 1 || Port > 65535) throw new ArgumentException("--port");
        string managed = Path.Combine(ServerPath, "SCPSL_Data", "Managed");
        if (!File.Exists(Path.Combine(managed, "LabApi.dll")) || !File.Exists(Path.Combine(managed, "Assembly-CSharp.dll")))
            throw new InstallerException("install.invalid_server");
        string policy = Path.Combine(ServerPath, "hoster_policy.txt");
        bool local = File.Exists(policy) && File.ReadAllLines(policy).Any(line => line.IndexOf("gamedir_for_configs: true", StringComparison.OrdinalIgnoreCase) >= 0 && !line.TrimStart().StartsWith("#", StringComparison.Ordinal));
        string appData = local ? Path.Combine(ServerPath, "AppData") : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(appData) && options.LabApiPath == null) throw new ArgumentException("--labapi-path");
        LabApiPath = Path.GetFullPath(options.LabApiPath ?? Path.Combine(appData, "SCP Secret Laboratory", "LabAPI"));
        ConfigPath = Path.Combine(LabApiPath, "configs", Port.ToString(System.Globalization.CultureInfo.InvariantCulture), "Ferrum", "config.yml");
        FerrumConfig existing = ReadConfig();
        FerrumPath = InstallPaths.ResolveRoot(options.FerrumPath ?? existing.FerrumDirectoryPath);
        if (options.FerrumPath != null && File.Exists(ConfigPath)
            && !string.Equals(InstallPaths.ResolveRoot(existing.FerrumDirectoryPath), FerrumPath,
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new ArgumentException("Existing ferrum_directory_path differs from --ferrum-path. Update the config explicitly first.");
        StateDirectory = InstallPaths.StateDirectory(FerrumPath, LabApiPath, Port);
    }

    public FerrumConfig ReadConfig()
    {
        if (!File.Exists(ConfigPath)) return new FerrumConfig();
        var config = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).IgnoreUnmatchedProperties()
            .Build().Deserialize<FerrumConfig>(File.ReadAllText(ConfigPath)) ?? throw new InstallerException("config.invalid", ConfigPath);
        ConfigTemplate.Validate(config); return config;
    }

    public void RequireStopped()
    {
        foreach (Process process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    if (process.Id == Environment.ProcessId) continue;
                    string name = process.ProcessName;
                    if (!name.StartsWith("SCPSL", StringComparison.OrdinalIgnoreCase) && !name.StartsWith("LocalAdmin", StringComparison.OrdinalIgnoreCase)) continue;
                    string? executable = process.MainModule?.FileName;
                    if (executable != null && Path.GetFullPath(executable).StartsWith(ServerPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                        throw new InstallerException("install.process_running");
                }
                catch (System.ComponentModel.Win32Exception) { }
                catch (InvalidOperationException) { }
            }
        }
    }
}
