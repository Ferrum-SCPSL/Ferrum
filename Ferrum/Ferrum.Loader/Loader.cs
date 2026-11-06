using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading;
using Ferrum.Contracts;
using Ferrum.Loader.Configuration;
using Ferrum.Loader.Features;
using Ferrum.Loader.Features.Logging;
using Ferrum.Loader.Localization;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Loader;
using LabApi.Loader.Features.Paths;
using LabApi.Loader.Features.Plugins;

namespace Ferrum.Loader;

/// <summary>The permanent, small LabAPI bootstrap. It never references versioned runtime assemblies.</summary>
public sealed class Loader : Plugin
{
    private readonly ConcurrentQueue<Action> mainThreadQueue = new();
    private FerrumConfig config = new();
    private LocalizationService messages = new("auto", "auto", null);
    private Bootstrapper? bootstrap;
    private bool validConfig;
    private SynchronizationContext? mainThread;
    public override string Name => "Ferrum";
    public override string Description => "Rust framework bootstrap with versioned runtime, localization and updates.";
    public override string Author => "Ferrum SCP:SL Team";
    public override Version RequiredApiVersion => new(1, 1, 7);

    public override void LoadConfigs()
    {
        string path = this.GetConfigPath("config.yml");
        try
        {
            config = ConfigurationReader.Read(path);
            string root = InstallPaths.ResolveRoot(config.FerrumDirectoryPath);
            messages = new LocalizationService(config.LogLanguage, config.PluginLanguage, Path.Combine(root, "Localizations"));
            validConfig = true;
        }
        catch (Exception error)
        {
            validConfig = false;
            Write("level.error", "config.invalid", path);
            if (config.Debug) ServerConsoleWriter.Write(error.ToString(), ConsoleColor.Gray);
        }
    }

    public override void Enable()
    {
        if (!validConfig || !config.IsEnabled || bootstrap != null) return;
        mainThread = SynchronizationContext.Current;
        try
        {
            string root = InstallPaths.ResolveRoot(config.FerrumDirectoryPath);
            var context = new RuntimeContext
            {
                Root = root, Config = config, Platform = InstallPaths.Platform,
                StateDirectory = InstallPaths.StateDirectory(root, PathManager.LabApi.FullName, GetPort()),
                WriteConsole = (message, color) => Post(() => ServerConsoleWriter.Write(message, color)),
                ScheduleRestart = RestartScheduler.Schedule,
            };
            bootstrap = new Bootstrapper();
            bootstrap.Enable(context, version => Write("level.warning", "update.rollback", version));
            ServerEvents.PluginsEnabled += PluginsEnabled;
            ServerEvents.RoundEnding += RoundEnding;
            ServerEvents.WaitingForPlayers += Drain;
        }
        catch (Exception error) { Failed(error); }
    }

    private void PluginsEnabled()
    {
        ServerEvents.PluginsEnabled -= PluginsEnabled;
        try { Drain(); bootstrap?.PluginsEnabled(); }
        catch (Exception error) { Failed(error); }
    }

    private void RoundEnding(RoundEndingEventArgs args) { Drain(); bootstrap?.RoundEnding(); Drain(); }
    private void Post(Action action)
    {
        if (SynchronizationContext.Current == mainThread && mainThread != null) action();
        else if (mainThread != null) mainThread.Post(_ => action(), null);
        else mainThreadQueue.Enqueue(action);
    }
    private void Drain() { while (mainThreadQueue.TryDequeue(out Action? action)) action(); }

    private void Failed(Exception error)
    {
        try { bootstrap?.Fail(); } catch (Exception stateError) { if (config.Debug) ServerConsoleWriter.Write(stateError.ToString(), ConsoleColor.Gray); }
        Unsubscribe();
        try { bootstrap?.Dispose(); } finally { bootstrap = null; }
        Write("level.error", "runtime.failed");
        if (config.Debug) ServerConsoleWriter.Write(error.ToString(), ConsoleColor.Gray);
    }

    private void Write(string level, string key, params object[] args) => ServerConsoleWriter.Write(
        "[" + messages.GetMessage(level) + "] [Ferrum] " + messages.GetMessage(key, args), level == "level.error" ? ConsoleColor.Red : ConsoleColor.Yellow);

    private static int GetPort()
    {
        Type type = typeof(LabApi.Features.Wrappers.Server);
        return Convert.ToInt32(type.GetProperty("Port", BindingFlags.Static | BindingFlags.Public)!.GetValue(null, null));
    }
    private void Unsubscribe()
    {
        ServerEvents.PluginsEnabled -= PluginsEnabled; ServerEvents.RoundEnding -= RoundEnding; ServerEvents.WaitingForPlayers -= Drain;
    }
    public override void Disable()
    {
        Unsubscribe();
        try { bootstrap?.Dispose(); } finally { bootstrap = null; Drain(); }
    }
}
