//#################################################################################################################################################
// Tool Oumijia — EPLAN Script Edition (v0.5.2)
//#################################################################################################################################################
// Nhập / Xuất EDZ và EPLAN Data Portal dùng cùng Oumijia.EplanUi.dll đã biên dịch.
// Script này không cần EPLAN API license. Người dùng tự đăng nhập vào Data Portal
// và tuân theo quyền, điều khoản, giấy phép của EPLAN.
//
// CÀI ĐẶT (một lần):
//   Chạy Install-Script.bat hoặc Install-Tool-Oumijia.bat.
//
// SỬ DỤNG:
//   Ribbon "Tool Oumijia" > nhóm "Quản lý EDZ":
//   Nhập EDZ… | Xuất EDZ… | Data Portal… | Menu bar
//
// LOG:
//   %APPDATA%\Tool Oumijia\logs\addin.log
//
// GHI CHÚ:
//   - Mọi xử lý UI chạy qua assemblies net48 trong %APPDATA%\Tool Oumijia\Portal.
//   - Script dùng Reflection để nạp chúng, nên EPLAN compiler không cần WebView2/SharpCompress.
//   - EDZ downloads được chuyển tiếp tới cùng màn hình Nhập EDZ với bản Add-in.
//#################################################################################################################################################

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.Gui;
using Eplan.EplApi.Scripting;

namespace Oumijia.EdzScript
{
    /// <summary>
    /// Điểm vào Script Edition: tạo ribbon + bốn lệnh Tool Oumijia.
    /// </summary>
    public class ToolOumijiaScript
    {
        private const string ScriptVersion = "0.5.2-script.1";
        private const string RibbonTabName = "Tool Oumijia";
        private const string CommandGroupName = "Quản lý EDZ";
        private const string ImportActionName = "OumijiaScriptImportEdz";
        private const string ExportActionName = "OumijiaScriptExportEdz";
        private const string DataPortalActionName = "OumijiaScriptDataPortal";

        private static bool _sharedResolverAttached;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        // ------------------------------------------------------------------ vòng đời script

        [DeclareRegister]
        public void Register()
        {
            AddinLog.Write("Script Register (version " + ScriptVersion + ")");
            EnsureRibbon("Register");
        }

        [DeclareUnregister]
        public void Unregister()
        {
            AddinLog.Write("Script Unregister");
        }

        [DeclareEventHandler("Eplan.EplApi.OnMainStart")]
        public void OnMainStart()
        {
            AddinLog.Write("Event OnMainStart");
            EnsureRibbon("OnMainStart");
        }

        [DeclareEventHandler("Eplan.EplApi.OnLoadWorkspace")]
        public void OnLoadWorkspace()
        {
            AddinLog.Write("Event OnLoadWorkspace");
            EnsureRibbon("OnLoadWorkspace");
        }

        [DeclareEventHandler("Eplan.EplApi.OnResetRibbon")]
        public void OnResetRibbon()
        {
            AddinLog.Write("Event OnResetRibbon");
            EnsureRibbon("OnResetRibbon");
        }

        [DeclareEventHandler("Eplan.EplApi.OnMainEnd")]
        public void OnMainEnd()
        {
            AddinLog.Write("Event OnMainEnd");
        }

        // ------------------------------------------------------------------ action

        [DeclareAction("OumijiaScriptImportEdz")]
        public void ImportEdz()
        {
            AddinLog.Write("Action OumijiaScriptImportEdz");
            OpenSharedForm("Oumijia.EplanUi.ImportEdzForm", null, "Không mở được hộp thoại Nhập EDZ");
        }

        [DeclareAction("OumijiaScriptExportEdz")]
        public void ExportEdz()
        {
            AddinLog.Write("Action OumijiaScriptExportEdz");
            OpenSharedForm("Oumijia.EplanUi.ExportEdzForm", null, "Không mở được hộp thoại Xuất EDZ");
        }

        /// <summary>Mở EPLAN Data Portal chính thức trong WebView2; người dùng tự đăng nhập.</summary>
        [DeclareAction("OumijiaScriptDataPortal")]
        public void DataPortal()
        {
            AddinLog.Write("Action OumijiaScriptDataPortal");
            object form = null;
            try
            {
                EnsureSharedResolver();
                string sharedDirectory = SharedDirectory();
                string hostPath = Path.Combine(sharedDirectory, "Oumijia.PortalHost.dll");
                if (!File.Exists(hostPath))
                {
                    ShowMissingShared(hostPath);
                    return;
                }

                Assembly assembly = Assembly.LoadFrom(hostPath);
                Type formType = assembly.GetType("Oumijia.PortalHost.PortalForm", true);
                string dataRoot = DataRoot();
                string userData = Path.Combine(dataRoot, "WebView2");
                string logDirectory = Path.Combine(dataRoot, "logs");
                form = Activator.CreateInstance(formType, new object[] { userData, null, logDirectory });

                MethodInfo showDialog = formType.GetMethod("ShowDialog", Type.EmptyTypes);
                showDialog.Invoke(form, null);

                List<string> downloaded = ReadDownloadedFiles(formType, form);
                bool requestImport = ReadBooleanProperty(formType, form, "RequestImport");
                AddinLog.Write("DataPortal closed: downloaded=" + downloaded.Count + ", requestImport=" + requestImport);

                IDisposable portalDialog = form as IDisposable;
                if (portalDialog != null) { portalDialog.Dispose(); }
                form = null;
                if (downloaded.Count == 0) return;

                bool openImport = requestImport;
                if (!openImport)
                {
                    DialogResult answer = MessageBox.Show(
                        "Đã tải " + downloaded.Count + " tệp từ EPLAN Data Portal.\n\n" +
                        "Mở hộp Nhập EDZ với các tệp này?",
                        "Tool Oumijia", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    openImport = answer == DialogResult.Yes;
                }
                if (!openImport) return;

                string[] files = downloaded.ToArray();
                OpenSharedForm("Oumijia.EplanUi.ImportEdzForm", files, "Không mở được hộp thoại Nhập EDZ");
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("DataPortal", ex);
                MessageBox.Show(
                    "Không mở được EPLAN Data Portal.\n\n" + ex.Message,
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                IDisposable disposable = form as IDisposable;
                if (disposable != null) { try { disposable.Dispose(); } catch { } }
            }
        }

        /// <summary>Kiểm tra script đã nạp và shared runtime có sẵn.</summary>
        [DeclareAction("OumijiaScriptSelfTest")]
        public void SelfTest()
        {
            if (!File.Exists(Path.Combine(SharedDirectory(), "Oumijia.EplanUi.dll")) ||
                !File.Exists(Path.Combine(SharedDirectory(), "Oumijia.EdzReader.dll")) ||
                !File.Exists(Path.Combine(SharedDirectory(), "Oumijia.PortalHost.dll")))
            {
                AddinLog.Write("SELFTEST ERROR - shared runtime is missing");
                return;
            }
            AddinLog.Write("SELFTEST OK - script loaded, shared UI loaded (version " + ScriptVersion + ")");
        }

        /// <summary>Tạo lại ribbon nếu tab bị mất (có thể gọi từ command line).</summary>
        [DeclareAction("OumijiaScriptEnsureRibbon")]
        public void EnsureRibbonAction()
        {
            EnsureRibbon("Action");
        }

        // ------------------------------------------------------------------ ribbon

        private static void EnsureRibbon(string source)
        {
            try
            {
                RibbonBar ribbon = new RibbonBar();
                RibbonTab tab = ribbon.GetTab(RibbonTabName);
                if (tab == null) tab = ribbon.AddTab(RibbonTabName);

                RibbonCommandGroup group = tab.GetCommandGroup(CommandGroupName);
                if (group == null) group = tab.AddCommandGroup(CommandGroupName);

                EnsureCommand(group, "Nhập EDZ…", ImportActionName);
                EnsureCommand(group, "Xuất EDZ…", ExportActionName);
                EnsureCommand(group, "Data Portal…", DataPortalActionName);
                EnsureCommand(group, "Menu bar", "MfToggleMainMenuAction");

                AddinLog.Write("Ribbon ready (" + source + ")");
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("EnsureRibbon(" + source + ")", ex);
            }
        }

        private static void EnsureCommand(RibbonCommandGroup group, string buttonText, string actionName)
        {
            try
            {
                if (group.GetCommand(actionName) == null) group.AddCommand(buttonText, actionName);
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("EnsureCommand(" + actionName + ")", ex);
            }
        }

        // ------------------------------------------------------------------ shared runtime

        private static string DataRoot()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tool Oumijia");
        }

        private static string SharedDirectory()
        {
            return Path.Combine(DataRoot(), "Portal");
        }

        private static void EnsureSharedResolver()
        {
            if (_sharedResolverAttached) return;
            try { AppDomain.CurrentDomain.AssemblyResolve += OnSharedAssemblyResolve; } catch { }
            try { SetDllDirectory(SharedDirectory()); } catch { }
            _sharedResolverAttached = true;
        }

        private static Assembly OnSharedAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                AssemblyName assemblyName = new AssemblyName(args.Name);
                string simpleName = assemblyName == null ? null : assemblyName.Name;
                if (String.IsNullOrEmpty(simpleName)) return null;

                bool isOurs =
                    simpleName.StartsWith("Oumijia.", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.StartsWith("Microsoft.Web.WebView2", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("SharpCompress", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Buffers", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Memory", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Numerics.Vectors", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Runtime.CompilerServices.Unsafe", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Text.Encoding.CodePages", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("System.Threading.Tasks.Extensions", StringComparison.OrdinalIgnoreCase) ||
                    simpleName.Equals("Microsoft.Bcl.AsyncInterfaces", StringComparison.OrdinalIgnoreCase);
                if (!isOurs) return null;

                string candidate = Path.Combine(SharedDirectory(), simpleName + ".dll");
                return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
            }
            catch { return null; }
        }

        private static void OpenSharedForm(string typeName, string[] downloadedFiles, string failureTitle)
        {
            try
            {
                EnsureSharedResolver();
                string assemblyPath = Path.Combine(SharedDirectory(), "Oumijia.EplanUi.dll");
                if (!File.Exists(assemblyPath))
                {
                    ShowMissingShared(assemblyPath);
                    return;
                }
                Assembly assembly = Assembly.LoadFrom(assemblyPath);
                Type formType = assembly.GetType(typeName, true);
                object form;
                if (downloadedFiles == null)
                {
                    form = Activator.CreateInstance(formType);
                }
                else
                {
                    form = Activator.CreateInstance(formType, new object[] { downloadedFiles });
                }
                MethodInfo showDialog = formType.GetMethod("ShowDialog", Type.EmptyTypes);
                showDialog.Invoke(form, null);
                IDisposable disposable = form as IDisposable;
                if (disposable != null) disposable.Dispose();
            }
            catch (Exception ex)
            {
                AddinLog.WriteError(typeName, ex);
                MessageBox.Show(
                    "Không mở được màn hình Tool Oumijia.\n\n" + ex.Message,
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static List<string> ReadDownloadedFiles(Type formType, object form)
        {
            List<string> files = new List<string>();
            try
            {
                PropertyInfo property = formType.GetProperty("DownloadedFiles");
                IEnumerable values = property == null ? null : property.GetValue(form, null) as IEnumerable;
                if (values == null) return files;
                foreach (object value in values)
                {
                    string path = value == null ? null : value.ToString();
                    if (!String.IsNullOrWhiteSpace(path)) files.Add(path);
                }
            }
            catch { }
            return files;
        }

        private static bool ReadBooleanProperty(Type formType, object form, string propertyName)
        {
            try
            {
                PropertyInfo property = formType.GetProperty(propertyName);
                if (property == null) return false;
                object value = property.GetValue(form, null);
                return value is bool && (bool)value;
            }
            catch { return false; }
        }

        private static void ShowMissingShared(string requiredPath)
        {
            MessageBox.Show(
                "Thiếu thành phần chạy của Tool Oumijia:\n" + requiredPath + "\n\n" +
                "Hãy chạy lại Install-Tool-Oumijia.bat để cài đầy đủ.",
                "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    //=================================================================================================================================================
    // Logging
    //=================================================================================================================================================

    internal static class AddinLog
    {
        private static readonly object Gate = new object();

        public static string LogDirectory
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tool Oumijia", "logs"); }
        }

        public static string FilePath
        {
            get { return Path.Combine(LogDirectory, "addin.log"); }
        }

        public static void Write(string message)
        {
            lock (Gate)
            {
                try
                {
                    Directory.CreateDirectory(LogDirectory);
                    File.AppendAllText(FilePath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message + Environment.NewLine, new UTF8Encoding(false));
                }
                catch { }
            }
        }

        public static void WriteError(string context, Exception exception)
        {
            Write(context + " ERROR: " + exception);
        }
    }
}
