using System;
using System.IO;
using System.Text;

namespace Oumijia.Core.Util;

/// <summary>Filesystem helpers shared across the tool.</summary>
public static class PathUtil
{
    /// <summary>Replaces invalid filename characters and trims trailing dots/spaces.</summary>
    public static string SanitizeFolderName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        var value = name!;
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(value.Length);
        foreach (var c in value.Trim())
        {
            var bad = false;
            foreach (var inv in invalid)
            {
                if (c == inv) { bad = true; break; }
            }
            sb.Append(bad ? '_' : c);
        }

        var result = sb.ToString().Trim().TrimEnd('.');
        while (result.Contains("  ")) result = result.Replace("  ", " ");
        return result;
    }

    /// <summary>
    /// Combines a root with a relative folder made of one or more segments
    /// ("Siemens/MCB"), sanitizing every segment. Empty segments are skipped.
    /// </summary>
    public static string CombineRelative(string root, string? relativeFolder)
    {
        var result = root;
        if (string.IsNullOrWhiteSpace(relativeFolder)) return result;

        var segments = relativeFolder!.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            var clean = SanitizeFolderName(segment);
            if (clean.Length == 0) continue;
            result = Path.Combine(result, clean);
        }
        return result;
    }

    /// <summary>Returns the relative folder part of a path below <paramref name="root"/>, or null.</summary>
    public static string? GetRelativeFolderOrNull(string root, string fullPath)
    {
        if (string.IsNullOrWhiteSpace(root)) return null;
        try
        {
            var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalizedPath = Path.GetFullPath(fullPath);
            if (!normalizedPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                !normalizedPath.StartsWith(normalizedRoot + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var rel = normalizedPath.Substring(normalizedRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var dir = Path.GetDirectoryName(rel);
            return string.IsNullOrEmpty(dir) ? null : dir.Replace(Path.DirectorySeparatorChar, '/');
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Finds a non-existing destination path by appending " (2)", " (3)"... before the extension.</summary>
    public static string UniqueDestination(string directory, string fileName)
    {
        var candidate = Path.Combine(directory, fileName);
        if (!File.Exists(candidate)) return candidate;

        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        for (var i = 2; i < 10000; i++)
        {
            candidate = Path.Combine(directory, baseName + " (" + i + ")" + ext);
            if (!File.Exists(candidate)) return candidate;
        }

        throw new IOException("Cannot find a free file name for: " + fileName);
    }

    public static string ExpandPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        try
        {
            return Environment.ExpandEnvironmentVariables(path);
        }
        catch
        {
            return path!;
        }
    }
}
