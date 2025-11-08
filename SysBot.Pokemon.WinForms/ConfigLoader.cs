using SysBot.Base;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;

namespace SysBot.Pokemon.WinForms;

public static class ConfigLoader
{
    public static readonly string WorkingDirectory = Environment.CurrentDirectory = Path.GetDirectoryName(Environment.ProcessPath)!;
    public static string ConfigPath { get; private set; } = Path.Combine(WorkingDirectory, "config.json");

    public static ProgramConfig LoadConfig(string? file)
    {
        if (file == null)
            file = ConfigPath;
        else
            ConfigPath = file;

        if (!TryGetConfig(file, out var cfg))
        {
            cfg = new ProgramConfig();
            cfg.Hub.Folder.CreateDefaults(WorkingDirectory);
        }

        LogConfig.MaxArchiveFiles = cfg.Hub.MaxArchiveFiles;
        LogConfig.LoggingEnabled = cfg.Hub.LoggingEnabled;
        return cfg;
    }

    private static bool TryGetConfig(string? file, [NotNullWhen(true)] out ProgramConfig? config)
    {
        config = null;
        if (!File.Exists(file))
            return false;
        try
        {

            var lines = File.ReadAllText(file);
            config = JsonSerializer.Deserialize(lines, ProgramConfigContext.Default.ProgramConfig);
            return config != null;
        }
        catch
        {
            return false;
        }
    }

    public static void Save(ProgramConfig cfg)
    {
        var lines = JsonSerializer.Serialize(cfg, ProgramConfigContext.Default.ProgramConfig);
        File.WriteAllText(ConfigPath, lines);
    }
}
