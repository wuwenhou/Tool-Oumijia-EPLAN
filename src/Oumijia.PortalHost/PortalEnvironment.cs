using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Oumijia.PortalHost
{
    /// <summary>
    /// Chuẩn bị môi trường nạp WebView2:
    ///  - đăng ký AssemblyResolve để tìm Microsoft.Web.WebView2.* cùng thư mục;
    ///  - SetDllDirectory để WebView2Loader.dll (native) được tìm thấy.
    ///
    /// Cần thiết vì EPLAN có thể nạp add-in/DLL từ thư mục shadow-copy khác với
    /// thư mục cài đặt, còn Script Edition nạp assembly bằng Assembly.LoadFrom.
    /// </summary>
    public static class PortalEnvironment
    {
        private static readonly object Gate = new object();
        private static bool _initialized;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        public static void Ensure()
        {
            lock (Gate)
            {
                if (_initialized) return;
                _initialized = true;

                try
                {
                    AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
                }
                catch
                {
                    // bỏ qua
                }

                try
                {
                    string directory = Path.GetDirectoryName(typeof(PortalEnvironment).Assembly.Location);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        SetDllDirectory(directory);
                    }
                }
                catch
                {
                    // bỏ qua
                }
            }
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                string simpleName = new AssemblyName(args.Name).Name;
                if (string.IsNullOrEmpty(simpleName)) return null;
                if (!simpleName.StartsWith("Microsoft.Web.WebView2", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                string directory = Path.GetDirectoryName(typeof(PortalEnvironment).Assembly.Location);
                if (string.IsNullOrEmpty(directory)) return null;

                string candidate = Path.Combine(directory, simpleName + ".dll");
                if (File.Exists(candidate))
                {
                    return Assembly.LoadFrom(candidate);
                }
            }
            catch
            {
                // bỏ qua
            }
            return null;
        }
    }
}
