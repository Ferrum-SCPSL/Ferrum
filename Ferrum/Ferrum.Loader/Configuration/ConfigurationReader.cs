using System.IO;
using System.Text;
using Ferrum.Contracts;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Ferrum.Loader.Configuration;

/// <summary>Unlike LabAPI's load-and-save helper, never rewrites an existing config.</summary>
public static class ConfigurationReader
{
    public static FerrumConfig Read(string path)
    {
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            writer.Write(ConfigTemplate.Create(new FerrumConfig()));
        }
        var config = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties().Build().Deserialize<FerrumConfig>(File.ReadAllText(path, Encoding.UTF8))
            ?? throw new InvalidDataException("Empty YAML configuration.");
        ConfigTemplate.Validate(config);
        return config;
    }
}
