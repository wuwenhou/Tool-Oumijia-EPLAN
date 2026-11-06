using System;
using System.IO;

namespace Oumijia.EplanUi
{
    /// <summary>
    /// Minimal file logger for the add-in (no external dependencies on purpose:
    /// the add-in must load inside EPLAN's shadow-copy environment).
    /// Log file: %APPDATA%\Tool Oumijia\logs\addin.log
    /// </summary>
    public static class AddinLog
    {
        private static readonly object Gate = new object();

        public static string LogDirectory
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Tool Oumijia",
                    "logs");
            }
        }

        public static string FilePath => Path.Combine(LogDirectory, "addin.log");

        public static void Write(string message)
        {
            lock (Gate)
            {
                try
                {
                    System.IO.Directory.CreateDirectory(LogDirectory);
                    File.AppendAllText(
                        FilePath,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message + Environment.NewLine);
                }
                catch
                {
                    // Logging must never break EPLAN.
                }
            }
        }

        public static void WriteError(string context, Exception exception)
        {
            Write(context + " ERROR: " + exception);
        }
    }
}
