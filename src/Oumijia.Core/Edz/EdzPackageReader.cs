using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Oumijia.Core.Edz;

public sealed class EdzReadOptions
{
    public int MaxParts { get; set; } = 50;
    public long MaxBytesPerPartXml { get; set; } = 5 * 1024 * 1024;
}

/// <summary>
/// Reads an EDZ package into a neutral <see cref="EdzPackageInfo"/> model:
/// manifest version, part master data, and an inventory of attachments
/// (macros / documents / DXF / images).
/// </summary>
public static class EdzPackageReader
{
    private static readonly Regex ManifestVersionRegex =
        new(@"<manifest[^>]*version\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase);

    public static EdzPackageInfo Read(string path, EdzReadOptions? options = null)
    {
        options ??= new EdzReadOptions();

        var info = new EdzPackageInfo { FilePath = path };
        using var archive = EdzArchive.Open(path);
        info.FileSizeBytes = archive.FileSizeBytes;
        info.ContainerKind = archive.ContainerKind;

        var entries = archive.GetEntries();
        foreach (var e in entries)
        {
            if (e.IsDirectory) continue;
            var ext = Path.GetExtension(e.Key).ToLowerInvariant();
            if (IsMacroExtension(ext)) info.MacroEntries.Add(e.Key);
            else if (ext == ".dxf") info.DxfEntries.Add(e.Key);
            else if (ext == ".pdf" || ext == ".doc" || ext == ".docx" || ext == ".xls" ||
                     ext == ".xlsx" || ext == ".txt" || ext == ".rtf") info.DocumentEntries.Add(e.Key);
            else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" ||
                     ext == ".gif" || ext == ".svg" || ext == ".tif" || ext == ".tiff") info.ImageEntries.Add(e.Key);
            else if (ext == ".xml") { /* part data is read below */ }
            else info.OtherDataEntries.Add(e.Key);
        }

        info.ManifestVersion = ReadManifestVersion(archive, entries);

        var candidates = SelectPartXmlEntries(entries);
        var parsed = 0;
        foreach (var key in candidates)
        {
            if (parsed >= options.MaxParts) break;
            var text = archive.ReadEntryText(key, options.MaxBytesPerPartXml);
            if (text == null) continue;

            EdzPartInfo? part = null;
            try
            {
                part = PartXmlParser.Parse(text, key);
            }
            catch
            {
                // Never let one broken XML break the whole read.
            }

            if (part != null)
            {
                info.Parts.Add(part);
                parsed++;
            }
        }

        return info;
    }

    private static string? ReadManifestVersion(EdzArchive archive, List<EdzArchiveEntry> entries)
    {
        var manifest = archive.ReadEntryText("manifest.xml", 1024 * 1024);
        if (manifest == null)
        {
            var entry = entries.FirstOrDefault(e =>
                !e.IsDirectory &&
                string.Equals(Path.GetFileName(e.Key), "manifest.xml", StringComparison.OrdinalIgnoreCase));
            if (entry != null) manifest = archive.ReadEntryText(entry.Key, 1024 * 1024);
        }

        if (manifest == null) return null;
        var m = ManifestVersionRegex.Match(manifest);
        return m.Success ? m.Groups[1].Value : null;
    }

    private static List<string> SelectPartXmlEntries(List<EdzArchiveEntry> entries)
    {
        var scored = new List<(string Key, int Score)>();
        foreach (var e in entries)
        {
            if (e.IsDirectory) continue;
            if (!e.Key.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) continue;
            if (string.Equals(Path.GetFileName(e.Key), "manifest.xml", StringComparison.OrdinalIgnoreCase)) continue;

            int score;
            if (e.Key.EndsWith(".part.xml", StringComparison.OrdinalIgnoreCase)) score = 3;
            else if (e.Key.IndexOf("partxml", StringComparison.OrdinalIgnoreCase) >= 0) score = 2;
            else if (e.Key.IndexOf("part", StringComparison.OrdinalIgnoreCase) >= 0) score = 1;
            else continue;

            scored.Add((e.Key, score));
        }

        scored.Sort((a, b) => b.Score.CompareTo(a.Score));
        return scored.Select(s => s.Key).ToList();
    }

    public static bool IsMacroExtension(string extension)
    {
        return extension.Equals(".ema", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".emp", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".ems", StringComparison.OrdinalIgnoreCase);
    }
}
