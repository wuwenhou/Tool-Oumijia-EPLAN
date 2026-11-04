using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Eplan.EplApi.Starter;

namespace Oumijia.Register
{
    /// <summary>
    /// Installs / uninstalls the Tool Oumijia EPLAN add-in by writing the
    /// EPLAN "API module" settings — without launching EPLAN.
    ///
    /// Settings written (see EPLAN config EplanEplApiModule.xml):
    ///   STATION.EplanEplApiModule.Module.Oumijia@EplAddin@EDZ.AssemblyName
    ///   ...AssemblyPath / ...TypeName / ...LoadOnStart
    ///
    /// The '.' inside the module name is escaped as '@' (EPLAN settings path syntax).
    ///
    /// IMPORTANT (JIT ordering): methods that touch Eplan.EplApi.Base types must only
    /// run AFTER SetEplanBinPath() has installed the assembly resolver. Keep the
    /// Write*/Read*/Delete* methods separate from FindEplanBin().
    /// </summary>
    internal static class Program
    {
        private const string AddinDllName = "Oumijia.EplAddin.EDZ.dll";
        private const string AddinTypeName = "Oumijia.EplanAddIn.AddIn";
        private const string ModuleSettingBase = "EplanEplApiModule.Module.Oumijia@EplAddin@EDZ";

        // STATION scope (matches EPLAN's own add-in registration schema).
        private const string SettingPrefix = "STATION.";

        private static int Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch
            {
                // Non-fatal on old consoles.
            }

            var command = args.Length > 0 ? args[0].ToLowerInvariant() : "install";

            try
            {
                switch (command)
                {
                    case "install":
                        return Install(args);
                    case "uninstall":
                        return Uninstall(args);
                    case "status":
                        return Status(args);
                    case "selftest":
                        return SelfTest(args);
                    case "-h":
                    case "--help":
                    case "help":
                        PrintUsage();
                        return 0;
                    default:
                        Console.Error.WriteLine("Lệnh không hợp lệ: " + command);
                        PrintUsage();
                        return 2;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("LỖI: " + ex.Message);
                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine("  -> " + ex.InnerException.Message);
                }
                return 1;
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Oumijia.Register — cài/gỡ add-in Tool Oumijia cho EPLAN");
            Console.WriteLine();
            Console.WriteLine("  Oumijia.Register install   [--bin <EPLAN Bin>] [--dll <addin dll>] [--user]");
            Console.WriteLine("  Oumijia.Register uninstall [--user]");
            Console.WriteLine("  Oumijia.Register status    [--user]");
            Console.WriteLine();
            Console.WriteLine("  --bin   thư mục Bin của EPLAN Platform (tự dò nếu bỏ trống)");
            Console.WriteLine("  --dll   đường dẫn Oumijia.EplAddin.EDZ.dll (mặc định: cạnh tool này)");
            Console.WriteLine("  --user  ghi vào phạm vi USER thay vì STATION (chỉ dùng khi cần)");
        }

        private static int Install(string[] args)
        {
            var explicitBin = GetArgValue(args, "--bin");
            var explicitDll = GetArgValue(args, "--dll");
            var userScope = HasFlag(args, "--user");

            var bin = FindEplanBin(explicitBin);
            if (bin == null)
            {
                Console.Error.WriteLine("Không tìm thấy EPLAN Platform (thư mục Bin chứa Eplan.EplApi.AFu.dll).");
                Console.Error.WriteLine("Hãy chỉ định:  Oumijia.Register install --bin \"C:\\Program Files\\EPLAN\\Platform\\<version>\\Bin\"");
                return 1;
            }
            Console.WriteLine("EPLAN Bin : " + bin);

            var sourceDll = explicitDll;
            if (string.IsNullOrWhiteSpace(sourceDll))
            {
                sourceDll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AddinDllName);
            }
            if (!File.Exists(sourceDll))
            {
                Console.Error.WriteLine("Không tìm thấy add-in dll: " + sourceDll);
                Console.Error.WriteLine("Hãy đặt " + AddinDllName + " cạnh tool này hoặc dùng --dll <path>.");
                return 1;
            }

            // Install add-in dll into its per-user home.
            var installDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Tool Oumijia", "AddIn");
            Directory.CreateDirectory(installDir);
            var installedDll = Path.Combine(installDir, AddinDllName);
            File.Copy(sourceDll, installedDll, true);
            Console.WriteLine("Add-in dll: " + installedDll);

            // Install resolver (must happen before touching Eplan.EplApi.Base types).
            var resolver = new AssemblyResolver();
            resolver.SetEplanBinPath(bin);
            try
            {
                resolver.PinToEplan();
            }
            catch
            {
                // Pinning is optional.
            }

            var assemblyFullName = AssemblyName.GetAssemblyName(installedDll).FullName;
            WriteRegistration(bin, installedDll, assemblyFullName, userScope);

            Console.WriteLine();
            Console.WriteLine("✔ Đã đăng ký add-in (phạm vi " + (userScope ? "USER" : "STATION") + ").");
            Console.WriteLine("  Khởi động lại EPLAN → ribbon 'Tool Oumijia' sẽ xuất hiện.");
            Console.WriteLine("  Kiểm tra bằng:  Oumijia.Register status");
            return 0;
        }

        private static int Uninstall(string[] args)
        {
            var userScope = HasFlag(args, "--user");
            var bin = FindEplanBin(GetArgValue(args, "--bin"));
            if (bin == null)
            {
                Console.Error.WriteLine("Không tìm thấy EPLAN Platform; không thể gỡ đăng ký (cần API để ghi settings).");
                return 1;
            }

            var resolver = new AssemblyResolver();
            resolver.SetEplanBinPath(bin);
            try
            {
                resolver.PinToEplan();
            }
            catch
            {
                // Optional.
            }

            DeleteRegistration(userScope);
            Console.WriteLine("✔ Đã gỡ đăng ký add-in khỏi EPLAN (restart EPLAN để áp dụng).");
            return 0;
        }

        private static int Status(string[] args)
        {
            var userScope = HasFlag(args, "--user");
            var bin = FindEplanBin(GetArgValue(args, "--bin"));
            if (bin == null)
            {
                Console.Error.WriteLine("Không tìm thấy EPLAN Platform.");
                return 1;
            }

            var resolver = new AssemblyResolver();
            resolver.SetEplanBinPath(bin);
            try
            {
                resolver.PinToEplan();
            }
            catch
            {
                // Optional.
            }

            return ReadRegistration(userScope);
        }

        // ------------------------------------------------------------------ file system

        private static string GetArgValue(string[] args, string name)
        {
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        private static bool HasFlag(string[] args, string name)
        {
            foreach (var a in args)
            {
                if (string.Equals(a, name, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static string FindEplanBin(string explicitPath)
        {
            if (!string.IsNullOrWhiteSpace(explicitPath) && IsEplanBin(explicitPath)) return explicitPath;

            var env = Environment.GetEnvironmentVariable("EPLAN_API_DIR");
            if (!string.IsNullOrWhiteSpace(env) && IsEplanBin(env)) return env;

            // Ask the EPLAN Starter where EPLAN lives.
            try
            {
                var finder = new EplanFinder();
                var installations = new List<EplanData>();
                finder.GetInstalledEplanVersions(ref installations);
                foreach (var data in installations)
                {
                    var candidates = new List<string>();
                    if (!string.IsNullOrWhiteSpace(data.EplanPath))
                    {
                        candidates.Add(data.EplanPath);
                        candidates.Add(Path.Combine(data.EplanPath, "Bin"));
                    }
                    foreach (var candidate in candidates)
                    {
                        if (IsEplanBin(candidate)) return candidate;
                    }
                }
            }
            catch
            {
                // Fall through to the directory scan.
            }

            // Scan Program Files for the newest Platform installation.
            try
            {
                var root = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "EPLAN", "Platform");
                if (Directory.Exists(root))
                {
                    string best = null;
                    foreach (var dir in Directory.GetDirectories(root))
                    {
                        var bin = Path.Combine(dir, "Bin");
                        if (!IsEplanBin(bin)) continue;
                        if (best == null || string.CompareOrdinal(dir, best) > 0) best = bin;
                    }
                    if (best != null) return best;
                }
            }
            catch
            {
                // No scan possible.
            }

            return null;
        }

        private static bool IsEplanBin(string path)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(path) &&
                       File.Exists(Path.Combine(path, "Eplan.EplApi.AFu.dll"));
            }
            catch
            {
                return false;
            }
        }

        // ------------------------------------------------------------------ selftest

        private static int SelfTest(string[] args)
        {
            var bin = FindEplanBin(GetArgValue(args, "--bin"));
            Console.WriteLine("EPLAN Bin: " + bin);
            if (bin == null) return 1;

            var p8Bin = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "EPLAN", "Electric P8", "2022.0.3", "Bin");
            Console.WriteLine("P8 Bin   : " + p8Bin + " (exists=" + Directory.Exists(p8Bin) + ")");

            var stationEod = @"C:\Users\Public\EPLAN\Settings\Electric P8\2022.0.3\x64\Cfg\STATION.Basic.DESKTOP-03FDK3P.xdb\Setting.eod";
            Console.WriteLine("STATION eod before: " + EodStamp(stationEod));

            TryVariant("A: SetEplanBinPath", r => r.SetEplanBinPath(bin));
            TryVariant("B: SetBinPathsWithAppMod", r => r.SetBinPathsWithAppMod(bin, "Electric P8"));
            if (Directory.Exists(p8Bin)) TryVariant("C: SetBinPaths(platform, p8)", r => r.SetBinPaths(bin, p8Bin));

            Console.WriteLine("STATION eod after : " + EodStamp(stationEod));
            return 0;
        }

        private static string EodStamp(string path)
        {
            try
            {
                return File.Exists(path) ? File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm:ss") : "(missing)";
            }
            catch
            {
                return "(error)";
            }
        }

        private static void TryVariant(string label, Action<AssemblyResolver> setup)
        {
            Console.WriteLine();
            Console.WriteLine("== " + label + " ==");
            try
            {
                var resolver = new AssemblyResolver();
                setup(resolver);
                try
                {
                    resolver.PinToEplan();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("  pin: " + ex.Message);
                }
                RunSettingsRoundTrip(label);
            }
            catch (Exception ex)
            {
                Console.WriteLine("  setup ERROR: " + ex.Message);
            }
        }

        private static void RunSettingsRoundTrip(string label)
        {
            var scratchPath = "STATION.OumijiaSelfTest." + label.Substring(0, 1) + ".Value1";
            var value = "hello-" + label;

            try
            {
                var settings = new Eplan.EplApi.Base.Settings();
                try
                {
                    if (!settings.ExistSetting(scratchPath))
                    {
                        settings.AddStringSetting(scratchPath, new string[0], new string[0],
                            Eplan.EplApi.Base.ISettings.CreationFlag.Insert);
                    }
                    settings.SetStringSetting(scratchPath, value);
                    Console.WriteLine("  same-instance read: " + TryGet(settings, scratchPath));
                }
                finally
                {
                    try { settings.Dispose(); } catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  write ERROR: " + ex.Message);
            }

            try
            {
                var again = new Eplan.EplApi.Base.Settings();
                try
                {
                    Console.WriteLine("  new-instance read : " + TryGet(again, scratchPath));
                }
                finally
                {
                    try { again.Dispose(); } catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  reread ERROR: " + ex.Message);
            }
        }

        // --------------------------------------------- settings (Base assembly territory)

        private static void WriteRegistration(string bin, string dllPath, string assemblyFullName, bool userScope)
        {
            var prefix = userScope ? "USER." : SettingPrefix;
            var settings = new Eplan.EplApi.Base.Settings();
            try
            {
                SetString(settings, prefix + ModuleSettingBase + ".AssemblyName", assemblyFullName);
                SetString(settings, prefix + ModuleSettingBase + ".AssemblyPath", dllPath);
                SetString(settings, prefix + ModuleSettingBase + ".TypeName", AddinTypeName);
                SetBool(settings, prefix + ModuleSettingBase + ".LoadOnStart", true);
            }
            finally
            {
                try { settings.Dispose(); } catch { }
            }

            Console.WriteLine("Đã ghi settings:");
            Console.WriteLine("  " + prefix + ModuleSettingBase + ".AssemblyPath = " + dllPath);
            Console.WriteLine("  " + prefix + ModuleSettingBase + ".AssemblyName = " + assemblyFullName);
            Console.WriteLine("  " + prefix + ModuleSettingBase + ".TypeName     = " + AddinTypeName);
        }

        private static void DeleteRegistration(bool userScope)
        {
            var prefix = userScope ? "USER." : SettingPrefix;
            var settings = new Eplan.EplApi.Base.Settings();
            try
            {
                Delete(settings, prefix + ModuleSettingBase + ".AssemblyName");
                Delete(settings, prefix + ModuleSettingBase + ".AssemblyPath");
                Delete(settings, prefix + ModuleSettingBase + ".TypeName");
                Delete(settings, prefix + ModuleSettingBase + ".LoadOnStart");
            }
            finally
            {
                try { settings.Dispose(); } catch { }
            }
        }

        private static int ReadRegistration(bool userScope)
        {
            var prefix = userScope ? "USER." : SettingPrefix;
            var settings = new Eplan.EplApi.Base.Settings();

            var path = prefix + ModuleSettingBase + ".AssemblyPath";
            var exists = false;
            try
            {
                exists = settings.ExistSetting(path);
            }
            catch
            {
                // Treat as missing.
            }

            Console.WriteLine("Phạm vi: " + (userScope ? "USER" : "STATION"));
            if (!exists)
            {
                Console.WriteLine("Trạng thái: CHƯA đăng ký");
                return 1;
            }

            Console.WriteLine("Trạng thái: ĐÃ đăng ký");
            Console.WriteLine("  AssemblyPath = " + TryGet(settings, prefix + ModuleSettingBase + ".AssemblyPath"));
            Console.WriteLine("  AssemblyName = " + TryGet(settings, prefix + ModuleSettingBase + ".AssemblyName"));
            Console.WriteLine("  TypeName     = " + TryGet(settings, prefix + ModuleSettingBase + ".TypeName"));
            Console.WriteLine("  LoadOnStart  = " + TryGetBool(settings, prefix + ModuleSettingBase + ".LoadOnStart"));
            return 0;
        }

        private static void SetString(Eplan.EplApi.Base.Settings settings, string path, string value)
        {
            try
            {
                if (!settings.ExistSetting(path))
                {
                    settings.AddStringSetting(path, new string[0], new string[0],
                        Eplan.EplApi.Base.ISettings.CreationFlag.Insert);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  (add setting: " + ex.Message + ")");
            }
            settings.SetStringSetting(path, value);
        }

        private static void SetBool(Eplan.EplApi.Base.Settings settings, string path, bool value)
        {
            try
            {
                if (!settings.ExistSetting(path))
                {
                    settings.AddBoolSetting(path, new bool[0], Eplan.EplApi.Base.ISettings.CreationFlag.Insert);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  (add setting: " + ex.Message + ")");
            }
            settings.SetBoolSetting(path, value);
        }

        private static void Delete(Eplan.EplApi.Base.Settings settings, string path)
        {
            try
            {
                if (settings.ExistSetting(path))
                {
                    settings.DeleteSetting(path);
                    Console.WriteLine("  - đã xóa: " + path);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  (delete: " + ex.Message + ")");
            }
        }

        private static string TryGet(Eplan.EplApi.Base.Settings settings, string path)
        {
            try
            {
                return settings.GetStringSetting(path, 0);
            }
            catch (Exception ex)
            {
                return "(không đọc được: " + ex.Message + ")";
            }
        }

        private static string TryGetBool(Eplan.EplApi.Base.Settings settings, string path)
        {
            try
            {
                return settings.GetBoolSetting(path, 0).ToString();
            }
            catch (Exception ex)
            {
                return "(không đọc được: " + ex.Message + ")";
            }
        }
    }
}
