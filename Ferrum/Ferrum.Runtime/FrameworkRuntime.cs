using System;
using System.IO;
using System.Threading;
using Ferrum.Contracts;
using Ferrum.Interop;
using Ferrum.Localization;
using Ferrum.Runtime.Features.Diagnostics;
using Ferrum.Runtime.Features.Logging;
using Ferrum.Updater;

namespace Ferrum.Runtime;

public sealed class FrameworkRuntime : IRuntime
{
    private RuntimeContext? context;
    private NativeBridge? bridge;
    private LocalizationService? messages;
    private Log? log;
    private UpdateCoordinator? updater;
    private ReleaseManifest? manifest;
    private bool enabled;
    private bool restartHandled;
    private int disposed;

    public void Initialize(RuntimeContext value)
    {
        context = value;
        string catalogs = Path.Combine(value.Root, "Localizations");
        LocalizationService.SeedCatalogs(catalogs);
        messages = new LocalizationService(value.Config.LogLanguage, value.Config.PluginLanguage, catalogs,
            path => value.WriteConsole(new LocalizationService(value.Config.LogLanguage, "auto", null).GetMessage("localization.invalid", path), ConsoleColor.Yellow));
        log = new Log(value, messages);
        manifest = JsonFile.Read<ReleaseManifest>(Path.Combine(value.RuntimeDirectory, "manifest.json"));
        bridge = new NativeBridge(Path.Combine(value.RuntimeDirectory, NativeBridge.LibraryFileName));
        bridge.ConfigureLocales(messages.LogLanguage, messages.PluginLanguage);
        BridgeProbe.Run(bridge);
        FerrumServices.Localization = messages;
        updater = new UpdateCoordinator(value, manifest.Version, (key, args) => log.Info(key, args), (key, error) => log.Warn(key, error), key => log.Debug(key));
    }

    public void PluginsEnabled()
    {
        if (Volatile.Read(ref disposed) != 0 || enabled) return;
        StartupBanner.Show(log!, messages!, manifest!.Version, manifest.Branch);
        context!.MarkReady();
        enabled = true;
        updater!.Start();
    }

    public void RoundEnding()
    {
        if (Volatile.Read(ref disposed) != 0 || !enabled || restartHandled || !context!.Config.RestartAfterUpdate || updater?.Ready != true) return;
        restartHandled = true;
        try { log!.Info(context.ScheduleRestart() ? "update.restart" : "update.restart_skipped"); }
        catch (Exception error) { log!.Warn("update.restart_skipped", error); }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        updater?.Dispose(); bridge?.Dispose();
        if (ReferenceEquals(FerrumServices.Localization, messages)) FerrumServices.Localization = null;
        log?.Info("runtime.disabled");
    }
}
