using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ferrum.Contracts;
using Ferrum.Localization;

namespace Ferrum.Installer;

[DataContract]
public sealed class InstallerRelease
{
    [DataMember(Name = "version", IsRequired = true)] public string Version { get; set; } = string.Empty;
}

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        string language = "auto";
        for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "--language") language = args[i + 1];
        var messages = new LocalizationService(language, language, null);
        InstallerOptions? options = null;
        try
        {
            options = InstallerOptions.Parse(args);
            if (options.Help) { Console.WriteLine(messages.GetMessage("install.help")); return 0; }
            using Stream source = typeof(Program).Assembly.GetManifestResourceStream("Ferrum.Release")!;
            using var reader = new StreamReader(source, Encoding.UTF8);
            string version = JsonFile.Parse<InstallerRelease>(reader.ReadToEnd()).Version;
            if (options.Version.Length == 0) options.Version = version;
            Console.WriteLine(messages.GetMessage("install.title", version));
            bool interactive = !Console.IsInputRedirected;
            if (string.IsNullOrWhiteSpace(options.ServerPath))
            {
                if (!interactive) throw new ArgumentException("--server-path");
                Console.Write(messages.GetMessage("install.server_prompt") + ": "); options.ServerPath = Console.ReadLine();
            }
            if (options.Port == 0)
            {
                if (!interactive) throw new ArgumentException("--port");
                Console.Write(messages.GetMessage("install.port_prompt") + ": "); options.Port = int.Parse(Console.ReadLine() ?? "0", System.Globalization.CultureInfo.InvariantCulture);
            }
            if (!options.ServerStopped)
            {
                if (!interactive) throw new InstallerException("install.stopped_required");
                Console.Write(messages.GetMessage("install.stopped_prompt") + ": ");
                if (!string.Equals(Console.ReadLine(), "yes", StringComparison.OrdinalIgnoreCase)) return 2;
            }
            var installation = new ServerInstallation(options);
            Console.WriteLine(messages.GetMessage("install.paths", installation.LabApiPath, installation.FerrumPath, installation.Port));
            using var cancel = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) => { eventArgs.Cancel = true; cancel.Cancel(); };
            await new InstallationService(installation, (key, values) => Console.WriteLine(messages.GetMessage(key, values)))
                .InstallAsync(options, cancel.Token).ConfigureAwait(false);
            return 0;
        }
        catch (Exception error)
        {
            string message = error.Data["Ferrum.Backup"] is string backup ? messages.GetMessage("install.restore_failed", backup)
                : error is InstallerException known ? messages.GetMessage(known.Key, known.Arguments)
                : error is ArgumentException || error is FormatException ? messages.GetMessage("install.invalid_arguments") : messages.GetMessage("install.failed");
            Console.Error.WriteLine(message);
            if (options?.Debug == true) Console.Error.WriteLine(error);
            return 1;
        }
    }
}
