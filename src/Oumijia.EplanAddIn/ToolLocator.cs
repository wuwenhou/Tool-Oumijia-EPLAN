using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Oumijia.EplanAddIn
{
    /// <summary>
    /// Locates the standalone Tool Oumijia executable so the add-in can open it.
    /// Search order: remembered path (tool-path.txt) → add-in folder → %APPDATA%\Tool Oumijia.
    /// When nothing is found the user picks the exe once and the path is remembered.
    /// </summary>
    internal static class ToolLocator
    {
        private const string ExeName = "Tool-Oumijia.exe";

        private static string ConfigFile
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Tool Oumijia",
                    "tool-path.txt");
            }
        }

        public static string FindToolExecutable()
        {
            try
            {
                if (File.Exists(ConfigFile))
                {
                    var remembered = File.ReadAllText(ConfigFile).Trim();
                    if (remembered.Length > 0 && File.Exists(remembered)) return remembered;
                }
            }
            catch
            {
                // Fall through to probing.
            }

            var candidates = new List<string>();

            try
            {
                var addinDir = Path.GetDirectoryName(typeof(ToolLocator).Assembly.Location);
                if (!string.IsNullOrEmpty(addinDir)) candidates.Add(Path.Combine(addinDir, ExeName));
            }
            catch
            {
                // Ignore.
            }

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            candidates.Add(Path.Combine(appData, "Tool Oumijia", ExeName));
            candidates.Add(Path.Combine(appData, "Tool Oumijia", "AddIn", ExeName));

            foreach (var candidate in candidates)
            {
                try
                {
                    if (File.Exists(candidate)) return candidate;
                }
                catch
                {
                    // Keep probing.
                }
            }

            return null;
        }

        public static string PromptForToolExecutable()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Chọn Tool-Oumijia.exe (bản standalone)";
                dialog.Filter = "Tool Oumijia (Tool-Oumijia.exe)|Tool-Oumijia.exe|Tất cả tệp (*.exe)|*.exe";
                if (dialog.ShowDialog() != DialogResult.OK) return null;

                try
                {
                    var dir = Path.GetDirectoryName(ConfigFile);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllText(ConfigFile, dialog.FileName);
                }
                catch
                {
                    // Remembering is best-effort.
                }

                return dialog.FileName;
            }
        }
    }
}
