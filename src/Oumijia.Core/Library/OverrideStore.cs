using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Oumijia.Core.Library;

/// <summary>
/// Remembers manual destination overrides, keyed by file name
/// (e.g. "ABB_S201-C16.edz" =&gt; "ABB/MCB"). Stored as overrides.json.
/// </summary>
public sealed class OverrideStore
{
    private readonly string _path;
    private readonly Dictionary<string, string> _map;

    private OverrideStore(string path, Dictionary<string, string> map)
    {
        _path = path;
        _map = map;
    }

    public string FilePath => _path;
    public int Count => _map.Count;

    public static OverrideStore Load(string path)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (loaded != null)
                {
                    foreach (var kv in loaded)
                    {
                        if (!string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
                            map[kv.Key] = kv.Value;
                    }
                }
            }
        }
        catch
        {
            // Corrupt overrides must never break the tool.
        }

        return new OverrideStore(path, map);
    }

    public string? Get(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        return _map.TryGetValue(fileName, out var value) ? value : null;
    }

    public void Set(string fileName, string? relativeFolder)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return;
        if (string.IsNullOrWhiteSpace(relativeFolder)) _map.Remove(fileName);
        else _map[fileName] = relativeFolder!;
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(_map, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_path, json);
        }
        catch
        {
            // Saving overrides is best-effort.
        }
    }
}
