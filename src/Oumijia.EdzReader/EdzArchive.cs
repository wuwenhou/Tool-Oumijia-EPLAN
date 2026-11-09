using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SharpCompress.Archives;

namespace Oumijia.EdzReader
{
    /// <summary>A single entry inside an EDZ package.</summary>
    public sealed class EdzArchiveEntry
    {
        public string Key { get; private set; }
        public long Size { get; private set; }
        public bool IsDirectory { get; private set; }

        public EdzArchiveEntry(string key, long size, bool isDirectory)
        {
            Key = key ?? String.Empty;
            Size = size;
            IsDirectory = isDirectory;
        }
    }

    /// <summary>Read-only view over an EDZ package (ZIP or 7z container).</summary>
    public sealed class EdzArchive : IDisposable
    {
        private const int DefaultMaxBytes = 20 * 1024 * 1024;

        private readonly IArchive _archive;

        public string FilePath { get; private set; }
        public long FileSizeBytes { get; private set; }
        public string ContainerKind { get; private set; }

        private EdzArchive(string path, IArchive archive, string containerKind, long size)
        {
            FilePath = path;
            _archive = archive;
            ContainerKind = containerKind;
            FileSizeBytes = size;
        }

        public static EdzArchive Open(string path)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", "path");
            if (!File.Exists(path)) throw new FileNotFoundException("EDZ file not found: " + path, path);

            string kind = DetectContainer(path);
            long size = new FileInfo(path).Length;
            IArchive archive = ArchiveFactory.OpenArchive(path, new SharpCompress.Readers.ReaderOptions());
            return new EdzArchive(path, archive, kind, size);
        }

        /// <summary>Detects whether the file uses a ZIP or 7z container (magic bytes).</summary>
        public static string DetectContainer(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                byte[] header = new byte[6];
                int n = fs.Read(header, 0, header.Length);
                if (n >= 4 && header[0] == 0x50 && header[1] == 0x4B) return "zip";
                if (n >= 6 && header[0] == 0x37 && header[1] == 0x7A && header[2] == 0xBC &&
                    header[3] == 0xAF && header[4] == 0x27 && header[5] == 0x1C) return "7z";
                return "unknown";
            }
        }

        public List<EdzArchiveEntry> GetEntries()
        {
            List<EdzArchiveEntry> list = new List<EdzArchiveEntry>();
            foreach (IArchiveEntry entry in _archive.Entries)
            {
                string key = entry.Key ?? String.Empty;
                list.Add(new EdzArchiveEntry(key, entry.Size, entry.IsDirectory));
            }
            return list;
        }

        /// <summary>Reads an entry fully into memory. Returns null when missing or larger than maxBytes.</summary>
        public byte[] ReadEntryBytes(string key, long maxBytes)
        {
            if (maxBytes <= 0) maxBytes = DefaultMaxBytes;
            if (String.IsNullOrEmpty(key)) return null;
            foreach (IArchiveEntry entry in _archive.Entries)
            {
                if (entry.IsDirectory) continue;
                if (!String.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase)) continue;

                using (Stream stream = entry.OpenEntryStream())
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] buffer = new byte[81920];
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
            }
            return null;
        }

        public string ReadEntryText(string key, long maxBytes)
        {
            byte[] bytes = ReadEntryBytes(key, maxBytes == 0 ? 5 * 1024 * 1024 : maxBytes);
            return bytes == null ? null : DecodeText(bytes);
        }

        /// <summary>
        /// Opens one entry for streaming (used by the EDZ filter to copy entries
        /// into a temporary package without loading large macros into memory).
        /// The caller must dispose the returned stream before disposing the archive.
        /// </summary>
        public Stream OpenEntryStream(string key)
        {
            if (String.IsNullOrEmpty(key)) return null;
            foreach (IArchiveEntry entry in _archive.Entries)
            {
                if (entry.IsDirectory) continue;
                if (!String.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase)) continue;
                return entry.OpenEntryStream();
            }
            return null;
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

        public void Dispose() { _archive.Dispose(); }
    }
}
