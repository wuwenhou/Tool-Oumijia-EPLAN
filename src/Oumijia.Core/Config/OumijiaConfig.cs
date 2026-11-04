using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Oumijia.Core.Classification;
using Oumijia.Core.Library;
using Oumijia.Core.Util;

namespace Oumijia.Core.Config;

/// <summary>Well-known file locations under %APPDATA%\Tool Oumijia.</summary>
public sealed class AppPaths
{
    public string BaseDirectory { get; }
    public string ConfigFile { get; }
    public string RulesFile { get; }
    public string OverridesFile { get; }
    public string LogDirectory { get; }

    public static AppPaths Create(string? baseDirectory = null)
    {
        var dir = string.IsNullOrWhiteSpace(baseDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tool Oumijia")
            : baseDirectory!;
        return new AppPaths(dir);
    }

    private AppPaths(string baseDirectory)
    {
        BaseDirectory = baseDirectory;
        ConfigFile = Path.Combine(baseDirectory, "config.json");
        RulesFile = Path.Combine(baseDirectory, "rules.json");
        OverridesFile = Path.Combine(baseDirectory, "overrides.json");
        LogDirectory = Path.Combine(baseDirectory, "logs");
    }

    public string NewExportLogPath() =>
        Path.Combine(LogDirectory, "export-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv");

    public string NewImportLogPath() =>
        Path.Combine(LogDirectory, "import-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv");
}

/// <summary>User configuration, stored as config.json.</summary>
public sealed class OumijiaConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public string LibraryRoot { get; set; } = DefaultLibraryRoot();

    public List<string> InboxFolders { get; set; } = new()
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
    };

    public bool MoveInsteadOfCopy { get; set; }

    /// <summary>"Rename" | "Skip" | "Overwrite" — see <see cref="ConflictPolicy"/>.</summary>
    public string ConflictPolicyName { get; set; } = "Rename";

    public string FallbackCategory { get; set; } = "_Unsorted";

    public List<string> MacroDirectories { get; set; } = new();

    public List<string> ScanExtensions { get; set; } = new() { ".edz", ".ema", ".emp", ".ems" };

    public static string DefaultLibraryRoot()
    {
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(docs, "Tool Oumijia", "Import");
    }

    public static OumijiaConfig Load(string configPath)
    {
        OumijiaConfig? cfg = null;
        try
        {
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                cfg = JsonSerializer.Deserialize<OumijiaConfig>(json, JsonOptions);
            }
        }
        catch
        {
            // Fall back to defaults on a broken config file.
        }

        cfg ??= new OumijiaConfig();
        cfg.ExpandAll();
        return cfg;
    }

    public void Save(string configPath)
    {
        try
        {
            var dir = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(configPath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch
        {
            // Saving is best-effort; the tool keeps working with in-memory values.
        }
    }

    public ConflictPolicy PolicyEnum
    {
        get
        {
            if (Enum.TryParse(ConflictPolicyName, true, out ConflictPolicy parsed)) return parsed;
            return ConflictPolicy.Rename;
        }
    }

    public static ClassifierRules LoadRules(string rulesPath)
    {
        try
        {
            if (File.Exists(rulesPath))
            {
                var json = File.ReadAllText(rulesPath);
                var loaded = JsonSerializer.Deserialize<ClassifierRules>(json, JsonOptions);
                if (loaded != null && loaded.Categories != null && loaded.Categories.Count > 0)
                {
                    NormalizeRules(loaded);
                    return loaded;
                }
            }
        }
        catch
        {
            // Fall through to defaults.
        }

        var defaults = ClassifierRules.CreateDefault();
        TryWriteDefaultRules(rulesPath, defaults);
        return defaults;
    }

    private static void NormalizeRules(ClassifierRules rules)
    {
        if (rules.ManufacturerAliases == null)
        {
            rules.ManufacturerAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in rules.ManufacturerAliases) copy[kv.Key] = kv.Value;
            rules.ManufacturerAliases = copy;
        }

        if (string.IsNullOrWhiteSpace(rules.FallbackCategory)) rules.FallbackCategory = "_Unsorted";
        if (string.IsNullOrWhiteSpace(rules.FallbackManufacturer)) rules.FallbackManufacturer = "_Unknown";
    }

    private static void TryWriteDefaultRules(string rulesPath, ClassifierRules defaults)
    {
        try
        {
            var dir = Path.GetDirectoryName(rulesPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            if (!File.Exists(rulesPath))
            {
                File.WriteAllText(rulesPath, JsonSerializer.Serialize(defaults, JsonOptions));
            }
        }
        catch
        {
            // Best-effort only.
        }
    }

    private void ExpandAll()
    {
        LibraryRoot = PathUtil.ExpandPath(LibraryRoot);
        for (var i = 0; i < InboxFolders.Count; i++) InboxFolders[i] = PathUtil.ExpandPath(InboxFolders[i]);
        for (var i = 0; i < MacroDirectories.Count; i++) MacroDirectories[i] = PathUtil.ExpandPath(MacroDirectories[i]);
    }
}
