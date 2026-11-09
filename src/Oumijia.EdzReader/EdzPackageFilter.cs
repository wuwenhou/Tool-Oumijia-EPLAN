using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Oumijia.EdzReader
{
    /// <summary>Result of building a per-part selection package.</summary>
    public sealed class EdzFilterResult
    {
        /// <summary>Original EDZ path.</summary>
        public string SourcePath { get; set; }

        /// <summary>File that must be passed to MDPartsManagement.ImportParts.</summary>
        public string FileToImport { get; set; }

        /// <summary>True when <see cref="FileToImport"/> is a generated temporary EDZ.</summary>
        public bool CreatedTempFile { get; set; }

        public int SelectedParts { get; set; }
        public int TotalParts { get; set; }

        /// <summary>Manifest item locators that were not found inside the archive.</summary>
        public List<string> MissingEntries { get; private set; }

        public EdzFilterResult()
        {
            MissingEntries = new List<string>();
        }
    }

    /// <summary>
    /// Builds a temporary EDZ that contains only the selected parts.
    ///
    /// EPLAN's ImportParts accepts whole files only, so per-part import means
    /// rewriting manifest.xml to keep the chosen &lt;package type="part"&gt;
    /// entries and copying just the files those packages reference.
    /// </summary>
    public static class EdzPackageFilter
    {
        public static EdzFilterResult Create(
            string sourcePath,
            ICollection<string> selectedPackageKeys,
            string tempDirectory)
        {
            if (String.IsNullOrWhiteSpace(sourcePath)) throw new ArgumentException("sourcePath is required.", "sourcePath");
            if (!File.Exists(sourcePath)) throw new FileNotFoundException("EDZ file not found: " + sourcePath, sourcePath);
            if (selectedPackageKeys == null || selectedPackageKeys.Count == 0)
                throw new ArgumentException("At least one part must be selected.", "selectedPackageKeys");

            HashSet<string> selected = new HashSet<string>(
                selectedPackageKeys.Where(k => !String.IsNullOrWhiteSpace(k)).Select(k => k.Trim()),
                StringComparer.OrdinalIgnoreCase);
            if (selected.Count == 0)
                throw new ArgumentException("At least one part must be selected.", "selectedPackageKeys");

            using (EdzArchive archive = EdzArchive.Open(sourcePath))
            {
                List<EdzArchiveEntry> entries = archive.GetEntries();
                string manifestText = ReadManifest(archive, entries);
                if (String.IsNullOrWhiteSpace(manifestText)) return Unfiltered(sourcePath);

                XDocument document;
                try { document = XDocument.Parse(manifestText); }
                catch { return Unfiltered(sourcePath); }

                XElement packages = document.Root == null ? null : document.Root.Element("packages");
                if (packages == null) return Unfiltered(sourcePath);

                List<XElement> allPackages = packages.Elements("package").ToList();
                HashSet<string> partKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (XElement package in allPackages)
                {
                    if (!IsType(package, "part")) continue;
                    string key = (string)package.Attribute("key");
                    if (!String.IsNullOrWhiteSpace(key)) partKeys.Add(key.Trim());
                }
                if (partKeys.Count == 0) return Unfiltered(sourcePath);

                int selectedCount = partKeys.Count(k => selected.Contains(k));
                if (selectedCount == 0)
                    throw new InvalidOperationException(
                        "Không tìm thấy part đã chọn trong manifest của EDZ: " + Path.GetFileName(sourcePath));

                // All parts selected: import the original file untouched.
                if (selectedCount >= partKeys.Count)
                {
                    EdzFilterResult original = Unfiltered(sourcePath);
                    original.SelectedParts = selectedCount;
                    original.TotalParts = partKeys.Count;
                    return original;
                }

                List<XElement> keptPackages = new List<XElement>();
                foreach (XElement package in allPackages)
                {
                    string key = (string)package.Attribute("key");
                    if (!String.IsNullOrWhiteSpace(key) && selected.Contains(key.Trim()))
                    {
                        keptPackages.Add(package);
                    }
                    else
                    {
                        package.Remove();
                    }
                }

                HashSet<string> wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (XElement package in keptPackages)
                {
                    foreach (XElement item in package.Descendants("item"))
                    {
                        string type = (string)item.Attribute("type");
                        string locator = (string)item.Attribute("locator");
                        string entryKey = ResolveEntryKey(entries, type, locator);
                        if (!String.IsNullOrWhiteSpace(entryKey)) wanted.Add(entryKey);
                    }
                }

                EdzFilterResult result = new EdzFilterResult();
                result.SourcePath = sourcePath;
                result.SelectedParts = selectedCount;
                result.TotalParts = partKeys.Count;

                // Keep each referenced file once, in the original archive order.
                List<EdzArchiveEntry> copies = new List<EdzArchiveEntry>();
                foreach (EdzArchiveEntry entry in entries)
                {
                    if (entry.IsDirectory) continue;
                    if (entry.Size <= 0) continue;
                    if (wanted.Contains(entry.Key)) copies.Add(entry);
                }

                // Report locators that could not be resolved, so the UI can warn.
                foreach (string locator in ReadLocators(keptPackages))
                {
                    bool found = false;
                    foreach (EdzArchiveEntry entry in copies)
                    {
                        if (entry.Key.EndsWith("/" + locator, StringComparison.OrdinalIgnoreCase) ||
                            entry.Key.Equals(locator, StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found && result.MissingEntries.Count < 50) result.MissingEntries.Add(locator);
                }

                byte[] manifestBytes = SaveManifest(document);
                if (String.IsNullOrWhiteSpace(tempDirectory))
                    tempDirectory = Path.Combine(Path.GetTempPath(), "ToolOumijia", "edz-filter");
                Directory.CreateDirectory(tempDirectory);

                string targetPath = Path.Combine(
                    tempDirectory,
                    "oumijia-" + SafeName(Path.GetFileNameWithoutExtension(sourcePath)) + "-" +
                    Guid.NewGuid().ToString("N").Substring(0, 8) + ".edz");

                try
                {
                    List<SevenZipCopyEntry> writes = new List<SevenZipCopyEntry>();
                    writes.Add(new SevenZipCopyEntry
                    {
                        Key = "manifest.xml",
                        Size = manifestBytes.Length,
                        Open = delegate { return new MemoryStream(manifestBytes, false); }
                    });

                    foreach (EdzArchiveEntry entry in copies)
                    {
                        string entryKey = entry.Key;
                        long entrySize = entry.Size;
                        writes.Add(new SevenZipCopyEntry
                        {
                            Key = entryKey,
                            Size = entrySize,
                            Open = delegate { return archive.OpenEntryStream(entryKey); }
                        });
                    }

                    SevenZipCopyWriter.Write(targetPath, writes);
                }
                catch
                {
                    try { if (File.Exists(targetPath)) File.Delete(targetPath); } catch { }
                    throw;
                }

                result.FileToImport = targetPath;
                result.CreatedTempFile = true;
                return result;
            }
        }

        private static EdzFilterResult Unfiltered(string sourcePath)
        {
            EdzFilterResult result = new EdzFilterResult();
            result.SourcePath = sourcePath;
            result.FileToImport = sourcePath;
            result.CreatedTempFile = false;
            return result;
        }

        private static bool IsType(XElement package, string type)
        {
            string value = (string)package.Attribute("type");
            return value != null && String.Equals(value.Trim(), type, StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadManifest(EdzArchive archive, List<EdzArchiveEntry> entries)
        {
            string manifest = archive.ReadEntryText("manifest.xml", 2 * 1024 * 1024);
            if (manifest != null) return manifest;
            foreach (EdzArchiveEntry entry in entries)
            {
                if (entry.IsDirectory) continue;
                if (!String.Equals(Path.GetFileName(entry.Key), "manifest.xml", StringComparison.OrdinalIgnoreCase)) continue;
                manifest = archive.ReadEntryText(entry.Key, 2 * 1024 * 1024);
                if (manifest != null) return manifest;
            }
            return null;
        }

        private static IEnumerable<string> ReadLocators(List<XElement> packages)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (XElement package in packages)
            {
                foreach (XElement item in package.Descendants("item"))
                {
                    string locator = ((string)item.Attribute("locator") ?? String.Empty)
                        .Replace('\\', '/').TrimStart('/');
                    if (locator.Length == 0) continue;
                    if (seen.Add(locator)) yield return locator;
                }
            }
        }

        private static string ResolveEntryKey(List<EdzArchiveEntry> entries, string type, string locator)
        {
            if (String.IsNullOrWhiteSpace(locator)) return null;
            string relative = locator.Replace('\\', '/').TrimStart('/');

            string folder = FolderForType(type);
            if (folder != null)
            {
                string candidate = folder + "/" + relative;
                foreach (EdzArchiveEntry entry in entries)
                {
                    if (entry.IsDirectory) continue;
                    if (String.Equals(entry.Key, candidate, StringComparison.OrdinalIgnoreCase)) return entry.Key;
                }
            }

            // Unknown item type: accept any entry with the same relative path.
            foreach (EdzArchiveEntry entry in entries)
            {
                if (entry.IsDirectory) continue;
                if (entry.Key.EndsWith("/" + relative, StringComparison.OrdinalIgnoreCase)) return entry.Key;
            }
            foreach (EdzArchiveEntry entry in entries)
            {
                if (entry.IsDirectory) continue;
                if (String.Equals(entry.Key, relative, StringComparison.OrdinalIgnoreCase)) return entry.Key;
            }
            return null;
        }

        private static string FolderForType(string type)
        {
            if (type == null) return null;
            switch (type.Trim().ToLowerInvariant())
            {
                case "partxml": return "items/partxml";
                case "picture": return "items/picture";
                case "document": return "items/document";
                case "macro": return "items/macro";
                case "dxf": return "items/dxf";
                default: return null;
            }
        }

        private static byte[] SaveManifest(XDocument document)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = new UTF8Encoding(false);
            settings.Indent = true;
            settings.NewLineHandling = NewLineHandling.None;

            using (MemoryStream stream = new MemoryStream())
            {
                using (XmlWriter writer = XmlWriter.Create(stream, settings))
                {
                    document.Save(writer);
                }
                return stream.ToArray();
            }
        }

        private static string SafeName(string name)
        {
            if (String.IsNullOrWhiteSpace(name)) return "edz";
            StringBuilder builder = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                if (Char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.') builder.Append(c);
                else builder.Append('_');
            }
            string result = builder.ToString().Trim('.', '_');
            if (result.Length == 0) result = "edz";
            if (result.Length > 60) result = result.Substring(0, 60);
            return result;
        }
    }
}
