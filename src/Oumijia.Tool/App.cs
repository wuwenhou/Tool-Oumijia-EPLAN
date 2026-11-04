using System;
using Oumijia.Core.Classification;
using Oumijia.Core.Config;

namespace Oumijia.Tool;

/// <summary>Shared application state for the standalone tool and the EPLAN add-in host.</summary>
public static class App
{
    private static AppPaths? _paths;

    public static AppPaths Paths => _paths ?? throw new InvalidOperationException("App.Initialize() has not been called.");

    public static OumijiaConfig Config { get; private set; } = new();

    public static Classifier Classifier { get; private set; } = new(ClassifierRules.CreateDefault());

    public static bool IsInitialized => _paths != null;

    public static void Initialize()
    {
        if (_paths != null) return;
        _paths = AppPaths.Create();
        Reload();
    }

    /// <summary>Reloads config + rules from disk (used after the settings tab is saved).</summary>
    public static void Reload()
    {
        if (_paths == null) _paths = AppPaths.Create();
        Config = OumijiaConfig.Load(_paths.ConfigFile);
        var rules = OumijiaConfig.LoadRules(_paths.RulesFile);
        Classifier = new Classifier(rules);
        Config.Save(_paths.ConfigFile);
    }

    public static string VersionText
    {
        get
        {
            try
            {
                var version = typeof(App).Assembly.GetName().Version;
                if (version == null) return "0.1.0";
                return version.Major + "." + version.Minor + "." + version.Build;
            }
            catch
            {
                return "0.1.0";
            }
        }
    }
}
