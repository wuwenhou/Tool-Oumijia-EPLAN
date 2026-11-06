using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Oumijia.PortalHost
{
    /// <summary>
    /// Đường dẫn, thiết lập và log dùng chung cho cửa sổ Data Portal.
    /// Không chứa credential; URL ghi log đã bỏ query/fragment.
    /// </summary>
    public static class PortalPaths
    {
        private static readonly object LogGate = new object();

        public static string AppDataRoot
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Tool Oumijia");
            }
        }

        public static string DefaultUserDataFolder
        {
            get { return Path.Combine(AppDataRoot, "WebView2"); }
        }

        public static string DefaultDownloadFolder
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Tool Oumijia_Downloads");
            }
        }

        public static string LogDirectory
        {
            get { return Path.Combine(AppDataRoot, "logs"); }
        }

        public static string SettingsFile
        {
            get { return Path.Combine(AppDataRoot, "portal-settings.txt"); }
        }

        /// <summary>Đọc thư mục tải đã lưu; trả về mặc định nếu chưa có.</summary>
        public static string LoadDownloadFolder()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string[] lines = File.ReadAllLines(SettingsFile);
                    if (lines.Length > 0)
                    {
                        string saved = (lines[0] ?? string.Empty).Trim();
                        if (saved.Length > 0)
                        {
                            return saved;
                        }
                    }
                }
            }
            catch
            {
                // best effort
            }
            return DefaultDownloadFolder;
        }

        public static void SaveDownloadFolder(string folder)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(folder)) return;
                Directory.CreateDirectory(AppDataRoot);
                File.WriteAllText(SettingsFile, folder.Trim() + Environment.NewLine, new UTF8Encoding(true));
            }
            catch
            {
                // best effort
            }
        }

        /// <summary>Bỏ query/fragment để không ghi token vào log.</summary>
        public static string SanitizeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;
            try
            {
                Uri uri = new Uri(url);
                return uri.GetLeftPart(UriPartial.Path);
            }
            catch
            {
                int cut = url.IndexOf('?');
                if (cut < 0) cut = url.IndexOf('#');
                return cut >= 0 ? url.Substring(0, cut) : url;
            }
        }

        /// <summary>Ghi một dòng CSV cho mỗi lượt tải (thư mục log mặc định).</summary>
        public static void AppendDownloadLog(string url, string filePath, string status, string detail)
        {
            AppendDownloadLog(LogDirectory, url, filePath, status, detail);
        }

        /// <summary>Ghi một dòng CSV cho mỗi lượt tải vào thư mục log chỉ định.</summary>
        public static void AppendDownloadLog(
            string logDirectory,
            string url,
            string filePath,
            string status,
            string detail)
        {
            lock (LogGate)
            {
                try
                {
                    string directory = string.IsNullOrWhiteSpace(logDirectory)
                        ? LogDirectory
                        : logDirectory.Trim();
                    Directory.CreateDirectory(directory);
                    string file = Path.Combine(
                        directory,
                        "data-portal-downloads-" + DateTime.Now.ToString("yyyyMMdd") + ".csv");

                    string size = string.Empty;
                    try
                    {
                        if (File.Exists(filePath))
                        {
                            size = new FileInfo(filePath).Length.ToString();
                        }
                    }
                    catch
                    {
                        // bỏ qua
                    }

                    var sb = new StringBuilder();
                    sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    AppendField(sb, SanitizeUrl(url));
                    AppendField(sb, filePath ?? string.Empty);
                    AppendField(sb, size);
                    AppendField(sb, status ?? string.Empty);
                    AppendField(sb, detail ?? string.Empty);

                    File.AppendAllText(file, sb + Environment.NewLine, new UTF8Encoding(true));
                }
                catch
                {
                    // Logging must never break a download.
                }
            }
        }

        private static void AppendField(StringBuilder sb, string value)
        {
            sb.Append(',');
            string v = value ?? string.Empty;
            if (v.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
            {
                sb.Append('"');
                sb.Append(v.Replace("\"", "\"\""));
                sb.Append('"');
            }
            else
            {
                sb.Append(v);
            }
        }

        /// <summary>Bỏ ký tự không hợp lệ trong tên file Windows.</summary>
        public static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "download.edz";

            var invalid = new HashSet<char>(Path.GetInvalidFileNameChars());
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                sb.Append(invalid.Contains(c) ? '_' : c);
            }

            string result = sb.ToString().Trim().TrimEnd('.');
            return result.Length == 0 ? "download.edz" : result;
        }

        /// <summary>Tạo tên file không trùng trong thư mục đích.</summary>
        public static string MakeUniquePath(string folder, string fileName)
        {
            string path = Path.Combine(folder, fileName);
            if (!File.Exists(path)) return path;

            string baseName = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            int index = 2;
            do
            {
                path = Path.Combine(folder, baseName + " (" + index + ")" + extension);
                index++;
            }
            while (File.Exists(path) && index < 10000);

            return path;
        }
    }
}
