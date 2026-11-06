using System;
using System.IO;
using System.Text;

namespace Oumijia.EplanUi
{
    /// <summary>
    /// Tiny CSV audit log for add-in operations (import/export).
    /// File: %APPDATA%\Tool Oumijia\logs\{operation}-yyyyMMdd.csv
    /// </summary>
    public static class AddinCsvLog
    {
        private static readonly object Gate = new object();

        public static void Append(string operation, params string[] fields)
        {
            lock (Gate)
            {
                try
                {
                    System.IO.Directory.CreateDirectory(AddinLog.LogDirectory);
                    var file = Path.Combine(
                        AddinLog.LogDirectory,
                        operation + "-" + DateTime.Now.ToString("yyyyMMdd") + ".csv");

                    var sb = new StringBuilder();
                    sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    foreach (var field in fields)
                    {
                        sb.Append(',');
                        sb.Append(Escape(field));
                    }

                    File.AppendAllText(file, sb + Environment.NewLine, new UTF8Encoding(true));
                }
                catch
                {
                    // Logging must never break the operation.
                }
            }
        }

        private static string Escape(string value)
        {
            var v = value ?? "";
            if (v.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
            {
                return "\"" + v.Replace("\"", "\"\"") + "\"";
            }
            return v;
        }
    }
}
