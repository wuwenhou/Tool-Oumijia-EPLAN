using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Oumijia.EdzReader
{
    public sealed class EdzReadOptions
    {
        public int MaxParts { get; set; }
        public long MaxBytesPerPartXml { get; set; }

        public EdzReadOptions()
        {
            MaxParts = 500;
            MaxBytesPerPartXml = 5 * 1024 * 1024;
        }
    }

    /// <summary>Reads an EDZ package into a neutral EdzPackageInfo model.</summary>
    public static class EdzPackageReader
    {
        private static readonly Regex ManifestVersionRegex = new Regex(
            @"<manifest[^>]*version\s*=\s*[""']([^""']+)[""']", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        public static EdzPackageInfo Read(string path)
        {
            return Read(path, new EdzReadOptions());
        }

        public static EdzPackageInfo Read(string path, EdzReadOptions options)
        {
            if (options == null) options = new EdzReadOptions();

            EdzPackageInfo info = new EdzPackageInfo();
            info.FilePath = path ?? String.Empty;
            using (EdzArchive archive = EdzArchive.Open(path))
            {
                info.FileSizeBytes = archive.FileSizeBytes;
                info.ContainerKind = archive.ContainerKind;
                List<EdzArchiveEntry> entries = archive.GetEntries();
                foreach (EdzArchiveEntry e in entries)
                {
                    if (e.IsDirectory) continue;
                    string ext = Path.GetExtension(e.Key);
                    if (String.IsNullOrEmpty(ext)) continue;
                    ext = ext.ToLowerInvariant();
                    if (IsMacroExtension(ext)) info.MacroEntries.Add(e.Key);
                    else if (ext == ".dxf") info.DxfEntries.Add(e.Key);
                    else if (ext == ".pdf" || ext == ".doc" || ext == ".docx" || ext == ".xls" ||
                             ext == ".xlsx" || ext == ".txt" || ext == ".rtf") info.DocumentEntries.Add(e.Key);
                    else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" ||
                             ext == ".gif" || ext == ".svg" || ext == ".tif" || ext == ".tiff") info.ImageEntries.Add(e.Key);
                    else if (ext == ".xml")
                    {
                        // Part data is read below.
                    }
                    else info.OtherDataEntries.Add(e.Key);
                }

                string manifest = ReadManifestText(archive, entries);
                info.ManifestVersion = ReadManifestVersionFromText(manifest);
                Dictionary<string, string> packageMap = ReadPartPackageMap(manifest);
                List<string> candidates = SelectPartXmlEntries(entries);
                int parsed = 0;
                foreach (string key in candidates)
                {
                    if (parsed >= options.MaxParts) break;
                    string text = archive.ReadEntryText(key, options.MaxBytesPerPartXml);
                    if (text == null) continue;
                    EdzPartInfo part = null;
                    try { part = PartXmlParser.Parse(text, key); } catch { /* Broken XML cannot break the whole read. */ }
                    if (part != null)
                    {
                        part.PackageKey = ResolvePackageKey(packageMap, key, part);
                        info.Parts.Add(part);
                        parsed++;
                    }
                }
                return info;
            }
        }

        private static string ReadManifestText(EdzArchive archive, List<EdzArchiveEntry> entries)
        {
            string manifest = archive.ReadEntryText("manifest.xml", 1024 * 1024);
            if (manifest != null) return manifest;
            foreach (EdzArchiveEntry entry in entries)
            {
                if (entry.IsDirectory) continue;
                if (!String.Equals(Path.GetFileName(entry.Key), "manifest.xml", StringComparison.OrdinalIgnoreCase)) continue;
                manifest = archive.ReadEntryText(entry.Key, 1024 * 1024);
                if (manifest != null) return manifest;
            }
            return null;
        }

        private static string ReadManifestVersionFromText(string manifest)
        {
            if (manifest == null) return null;
            Match m = ManifestVersionRegex.Match(manifest);
            return m.Success ? m.Groups[1].Value : null;
        }

        /// <summary>
        /// Maps "items/partxml/&lt;locator&gt;" to the manifest part package key.
        /// The key is what EPLAN uses to identify one part inside a multi-part EDZ.
        /// </summary>
        private static Dictionary<string, string> ReadPartPackageMap(string manifest)
        {
            Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (String.IsNullOrWhiteSpace(manifest)) return map;
            try
            {
                System.Xml.Linq.XDocument doc = System.Xml.Linq.XDocument.Parse(manifest);
                foreach (System.Xml.Linq.XElement package in doc.Descendants("package"))
                {
                    string type = (string)package.Attribute("type");
                    if (!String.Equals(type, "part", StringComparison.OrdinalIgnoreCase)) continue;
                    string key = (string)package.Attribute("key");
                    if (String.IsNullOrWhiteSpace(key)) continue;

                    foreach (System.Xml.Linq.XElement item in package.Descendants("item"))
                    {
                        string itemType = (string)item.Attribute("type");
                        string itemName = (string)item.Attribute("name");
                        if (!String.Equals(itemType, "partxml", StringComparison.OrdinalIgnoreCase)) continue;
                        if (!String.Equals(itemName, "part", StringComparison.OrdinalIgnoreCase)) continue;
                        string locator = (string)item.Attribute("locator");
                        if (String.IsNullOrWhiteSpace(locator)) continue;
                        string entryKey = "items/partxml/" + locator.Replace('\\', '/').TrimStart('/');
                        map[entryKey] = key.Trim();
                    }
                }
            }
            catch
            {
                // A broken manifest only disables per-part filtering, not preview.
            }
            return map;
        }

        private static string ResolvePackageKey(Dictionary<string, string> map, string entryKey, EdzPartInfo part)
        {
            string key;
            if (map != null && map.TryGetValue(entryKey ?? String.Empty, out key) && !String.IsNullOrWhiteSpace(key))
                return key;
            return FirstNonEmpty(part.PartNumber, part.TypeNumber, part.OrderNumber);
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (string value in values)
                if (!String.IsNullOrWhiteSpace(value)) return value.Trim();
            return null;
        }

        private static List<string> SelectPartXmlEntries(List<EdzArchiveEntry> entries)
        {
            List<KeyValuePair<string, int>> scored = new List<KeyValuePair<string, int>>();
            foreach (EdzArchiveEntry e in entries)
            {
                if (e.IsDirectory) continue;
                if (!e.Key.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)) continue;
                if (String.Equals(Path.GetFileName(e.Key), "manifest.xml", StringComparison.OrdinalIgnoreCase)) continue;

                int score;
                if (e.Key.EndsWith(".part.xml", StringComparison.OrdinalIgnoreCase)) score = 3;
                else if (e.Key.IndexOf("partxml", StringComparison.OrdinalIgnoreCase) >= 0) score = 2;
                else if (e.Key.IndexOf("part", StringComparison.OrdinalIgnoreCase) >= 0) score = 1;
                else continue;

                scored.Add(new KeyValuePair<string, int>(e.Key, score));
            }

            scored.Sort(delegate (KeyValuePair<string, int> a, KeyValuePair<string, int> b)
            {
                int compare = b.Value - a.Value;
                return compare != 0 ? compare : String.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase);
            });

            List<string> keys = new List<string>();
            foreach (KeyValuePair<string, int> item in scored) keys.Add(item.Key);
            return keys;
        }

        public static bool IsMacroExtension(string extension)
        {
            if (extension == null) return false;
            return String.Equals(extension, ".ema", StringComparison.OrdinalIgnoreCase) ||
                   String.Equals(extension, ".emp", StringComparison.OrdinalIgnoreCase) ||
                   String.Equals(extension, ".ems", StringComparison.OrdinalIgnoreCase);
        }
    }
}
