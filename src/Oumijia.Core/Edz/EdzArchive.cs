using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SharpCompress.Archives;
using SharpCompress.Readers;

namespace Oumijia.Core.Edz;

/// <summary>A single entry inside an EDZ package.</summary>
public sealed class EdzArchiveEntry
{
    public string Key { get; }
    public long Size { get; }
    public bool IsDirectory { get; }

    public EdzArchiveEntry(string key, long size, bool isDirectory)
    {
        Key = key;
        Size = size;
        IsDirectory = isDirectory;
    }
}

/// <summary>
/// Read-only view over an EDZ package (ZIP or 7z container).
/// EDZ = "EPLAN Data Zip": an archive carrying part master data
/// (manifest.xml + items/partxml + attachments such as macros, images, PDFs).
/// </summary>
public sealed class EdzArchive : IDisposable
{
    private const int DefaultMaxBytes = 20 * 1024 * 1024;

    private readonly IArchive _archive;

    public string FilePath { get; }
    public long FileSizeBytes { get; }
    public string ContainerKind { get; }

    private EdzArchive(string path, IArchive archive, string containerKind, long size)
    {
        FilePath = path;
        _archive = archive;
        ContainerKind = containerKind;
        FileSizeBytes = size;
    }

    public static EdzArchive Open(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException("EDZ file not found: " + path, path);

        var kind = DetectContainer(path);
        var size = new FileInfo(path).Length;
        var archive = ArchiveFactory.OpenArchive(path, new ReaderOptions());
        return new EdzArchive(path, archive, kind, size);
    }

    /// <summary>Detects whether the file uses a ZIP or 7z container (magic bytes).</summary>
    public static string DetectContainer(string path)
    {
        using var fs = File.OpenRead(path);
        var header = new byte[6];
        var n = fs.Read(header, 0, header.Length);
        if (n >= 4 && header[0] == 0x50 && header[1] == 0x4B) return "zip";
        if (n >= 6 && header[0] == 0x37 && header[1] == 0x7A && header[2] == 0xBC &&
            header[3] == 0xAF && header[4] == 0x27 && header[5] == 0x1C) return "7z";
        return "unknown";
    }

    public List<EdzArchiveEntry> GetEntries()
    {
        var list = new List<EdzArchiveEntry>();
        foreach (var entry in _archive.Entries)
        {
            var key = entry.Key ?? string.Empty;
            list.Add(new EdzArchiveEntry(key, entry.Size, entry.IsDirectory));
        }
        return list;
    }

    /// <summary>Reads an entry fully into memory. Returns null when missing or larger than <paramref name="maxBytes"/>.</summary>
    public byte[]? ReadEntryBytes(string key, long maxBytes = DefaultMaxBytes)
    {
        if (string.IsNullOrEmpty(key)) return null;
        foreach (var entry in _archive.Entries)
        {
            if (entry.IsDirectory) continue;
            if (!string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase)) continue;

            using var stream = entry.OpenEntryStream();
            using var ms = new MemoryStream();
            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                total += read;
                if (total > maxBytes) return null;
                ms.Write(buffer, 0, read);
            }
            return ms.ToArray();
        }
        return null;
    }

    public string? ReadEntryText(string key, long maxBytes = 5 * 1024 * 1024)
    {
        var bytes = ReadEntryBytes(key, maxBytes);
        return bytes == null ? null : DecodeText(bytes);
    }

    /// <summary>Decodes text with BOM detection and a non-throwing UTF-8 fallback.</summary>
    public static string DecodeText(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
        return new UTF8Encoding(false, false).GetString(bytes);
    }

    public void Dispose() => _archive.Dispose();
}
