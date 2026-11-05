//#################################################################################################################################################
// Tool Oumijia — EPLAN Script Edition (v0.5.0)
//#################################################################################################################################################
// Nhập / Xuất gói dữ liệu linh kiện EPLAN (*.edz) trực tiếp trong EPLAN.
//
// Script này thay thế add-in "Tool Oumijia" (add-in chưa ký bị EPLAN chặn khi máy
// không có EPLAN API license). Script chạy bằng EPLAN Scripting nên KHÔNG cần API
// license, KHÔNG cần ký assembly, và dùng được trên nhiều phiên bản EPLAN.
//
// CÀI ĐẶT (một lần):
//   Cách khuyến nghị: chạy Install-Script.bat (tự đăng ký qua RegisterScript).
//   Hoặc thủ công: File > Extras > Interfaces > Scripts > chọn file này, sau đó
//   đăng ký để EPLAN tự nạp lại ở mọi lần khởi động.
//
// SỬ DỤNG:
//   Ribbon "Tool Oumijia" > nhóm "Quản lý EDZ" > "Nhập EDZ…" / "Xuất EDZ…"
//
// LOG:
//   %APPDATA%\Tool Oumijia\logs\addin.log
//   %APPDATA%\Tool Oumijia\logs\import-yyyyMMdd.csv
//   %APPDATA%\Tool Oumijia\logs\export-yyyyMMdd.csv
//
// GHI CHÚ KỸ THUẬT:
//   - Toàn bộ mã dùng cú pháp C# 5 để tương thích cả EPLAN cũ (2022) lẫn mới.
//   - Ribbon được tạo lại ở các sự kiện OnMainStart / OnLoadWorkspace / OnResetRibbon
//     nên tab luôn xuất hiện dù EPLAN có nạp lại workspace.
//     (OnLoadWorkspace/OnResetRibbon chỉ có ở các bản EPLAN mới — vô hại khi vắng.)
//   - [DeclareRegister] chạy khi Load script; [DeclareEventHandler] chạy khi khởi động.
//   - Nhập EDZ: field-mapping scheme được tự dò (EPLAN từ chối scheme rỗng — S030060).
//   - Xuất EDZ: "Một file EDZ" chỉ chọn THƯ MỤC; tên file tự động theo mã sản phẩm
//     (1 part) hoặc parts.edz (nhiều part); nhớ thư mục đã chọn.
//#################################################################################################################################################

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.Base;
using Eplan.EplApi.Gui;
using Eplan.EplApi.MasterData;
using Eplan.EplApi.Scripting;

#pragma warning disable 618

namespace Oumijia.EdzScript
{
    /// <summary>
    /// Điểm vào của script: tạo ribbon + đăng ký 2 action Nhập/Xuất EDZ.
    /// </summary>
    public class ToolOumijiaScript
    {
        private const string ScriptVersion = "0.6.0-script.1";
        private const string RibbonTabName = "Tool Oumijia";
        private const string CommandGroupName = "Quản lý EDZ";
        private const string ClassicCommandGroupName = "EPLAN Classic";
        private const string VeeCommandGroupName = "VEE";
        private const string GridCommandGroupName = "GIRD";
        private const string ImportActionName = "OumijiaScriptImportEdz";
        private const string ExportActionName = "OumijiaScriptExportEdz";
        private const string ClassicActionName = "OumijiaScriptEplanClassic";
        private const string VeeExportActionName = "OumijiaScriptVeeExportConnections";
        private const string VeeTableMakerActionName = "OumijiaScriptVeeTableMaker";
        private const string MenuBarActionName = "OumijiaScriptToggleMenuBar";
        private const string GridSnapActionName = "OumijiaScriptToggleGridSnap";
        private const string ObjectSnapActionName = "OumijiaScriptToggleObjectSnap";
        private const string GridVisibilityActionName = "OumijiaScriptToggleGridVisibility";
        private const string CursorDesignActionName = "OumijiaScriptToggleCursorDesign";
        private const string ArrangeOnGridActionName = "OumijiaScriptArrangeOnGrid";

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

        // Script đã Load được EPLAN tự nạp lại khi khởi động, nhưng [DeclareRegister]
        // chỉ chạy lúc Load tay. Vì vậy ribbon được tạo lại từ các sự kiện hệ thống.
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
            try
            {
                using (ImportEdzForm form = new ImportEdzForm())
                {
                    form.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("ImportEdz", ex);
                MessageBox.Show(
                    "Không mở được hộp thoại Nhập EDZ:\n" + ex.Message,
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [DeclareAction("OumijiaScriptExportEdz")]
        public void ExportEdz()
        {
            AddinLog.Write("Action OumijiaScriptExportEdz");
            try
            {
                using (ExportEdzForm form = new ExportEdzForm())
                {
                    form.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("ExportEdz", ex);
                MessageBox.Show(
                    "Không mở được hộp thoại Xuất EDZ:\n" + ex.Message,
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Mở cửa sổ EPLAN Classic (menu cũ).</summary>
        [DeclareAction("OumijiaScriptEplanClassic")]
        public void EplanClassic()
        {
            AddinLog.Write("Action " + ClassicActionName);
            if (!EplanClassicLauncher.Open())
            {
                MessageBox.Show(
                    "Không mở được EPLAN Classic. Chi tiết xem addin.log.",
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Gọi VEE Export connections nếu đã cài; nếu chưa cài thì hướng dẫn.</summary>
        [DeclareAction("OumijiaScriptVeeExportConnections")]
        public void VeeExportConnections()
        {
            AddinLog.Write("Action " + VeeExportActionName);
            VeeIntegration.OpenExportConnections();
        }

        /// <summary>Gọi VEE Table Maker nếu đã cài; nếu chưa cài thì hướng dẫn.</summary>
        [DeclareAction("OumijiaScriptVeeTableMaker")]
        public void VeeTableMaker()
        {
            AddinLog.Write("Action " + VeeTableMakerActionName);
            VeeIntegration.OpenTableMaker();
        }

        [DeclareAction("OumijiaScriptToggleMenuBar")]
        public void ToggleMenuBar()
        {
            InvokeBuiltIn(MenuBarActionName, "MfToggleMainMenuAction", "Main menu bar");
        }

        [DeclareAction("OumijiaScriptToggleGridSnap")]
        public void ToggleGridSnap()
        {
            InvokeBuiltIn(GridSnapActionName, "XGedToggleGridSnapAction", "Grid snap");
        }

        [DeclareAction("OumijiaScriptToggleObjectSnap")]
        public void ToggleObjectSnap()
        {
            InvokeBuiltIn(ObjectSnapActionName, "XGedToggleObjectSnapAction", "Object snap");
        }

        [DeclareAction("OumijiaScriptToggleGridVisibility")]
        public void ToggleGridVisibility()
        {
            InvokeBuiltIn(GridVisibilityActionName, "XGedToggleGridVisibilityAction", "Grid visibility");
        }

        [DeclareAction("OumijiaScriptToggleCursorDesign")]
        public void ToggleCursorDesign()
        {
            InvokeBuiltIn(CursorDesignActionName, "XGedToggleCursorDesignAction", "Design mode");
        }

        [DeclareAction("OumijiaScriptArrangeOnGrid")]
        public void ArrangeOnGrid()
        {
            InvokeBuiltIn(ArrangeOnGridActionName, "XGedIaArrangeOnGrid", "Align on grid");
        }

        /// <summary>Dùng để kiểm tra script đã nạp và action gọi được (chạy từ command line).</summary>
        [DeclareAction("OumijiaScriptSelfTest")]
        public void SelfTest()
        {
            AddinLog.Write("SELFTEST OK - script loaded, actions callable (version " + ScriptVersion + ")");
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
                if (tab == null)
                {
                    tab = ribbon.AddTab(RibbonTabName);
                }

                RibbonCommandGroup group = GetOrAddCommandGroup(tab, CommandGroupName);
                EnsureCommand(group, "Nhập EDZ…", ImportActionName);
                EnsureCommand(group, "Xuất EDZ…", ExportActionName);

                RibbonCommandGroup classicGroup = GetOrAddCommandGroup(tab, ClassicCommandGroupName);
                EnsureCommand(classicGroup, "EPLAN Classic…", ClassicActionName);

                RibbonCommandGroup veeGroup = GetOrAddCommandGroup(tab, VeeCommandGroupName);
                EnsureCommand(veeGroup, "VEE Export connections…", VeeExportActionName);
                EnsureCommand(veeGroup, "VEE Table Maker…", VeeTableMakerActionName);

                RibbonCommandGroup gridGroup = GetOrAddCommandGroup(tab, GridCommandGroupName);
                EnsureCommand(gridGroup, "Menu bar", MenuBarActionName);
                EnsureCommand(gridGroup, "Grid snap", GridSnapActionName);
                EnsureCommand(gridGroup, "Object snap", ObjectSnapActionName);
                EnsureCommand(gridGroup, "Grid view", GridVisibilityActionName);
                EnsureCommand(gridGroup, "Design mode", CursorDesignActionName);
                EnsureCommand(gridGroup, "Align on grid", ArrangeOnGridActionName);

                AddinLog.Write("Ribbon ready (" + source + ")");
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("EnsureRibbon(" + source + ")", ex);
            }
        }

        private static RibbonCommandGroup GetOrAddCommandGroup(RibbonTab tab, string name)
        {
            RibbonCommandGroup group = tab.GetCommandGroup(name);
            if (group == null)
            {
                group = tab.AddCommandGroup(name);
            }
            return group;
        }

        private static bool InvokeBuiltIn(string actionName, string builtInAction, string displayName)
        {
            try
            {
                ActionManager manager = new ActionManager();
                Eplan.EplApi.ApplicationFramework.Action action = manager.FindAction(builtInAction);
                if (action == null)
                {
                    AddinLog.Write(actionName + ": action not found: " + builtInAction);
                    return false;
                }

                bool ok = action.Execute(new ActionCallingContext());
                AddinLog.Write(actionName + ": invoked " + displayName + " (" + builtInAction + ", ok=" + ok + ")");
                return ok;
            }
            catch (Exception ex)
            {
                AddinLog.WriteError(actionName + " -> " + builtInAction, ex);
                return false;
            }
        }

        private static void EnsureCommand(RibbonCommandGroup group, string buttonText, string actionName)
        {
            try
            {
                if (group.GetCommand(actionName) == null)
                {
                    group.AddCommand(buttonText, actionName);
                }
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("EnsureCommand(" + actionName + ")", ex);
            }
        }
    }

    //=================================================================================================================================================
    // Logging
    //=================================================================================================================================================

    /// <summary>
    /// Minimal file logger: %APPDATA%\Tool Oumijia\logs\addin.log
    /// </summary>
    internal static class AddinLog
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

    /// <summary>
    /// CSV audit log: %APPDATA%\Tool Oumijia\logs\{operation}-yyyyMMdd.csv
    /// </summary>
    internal static class AddinCsvLog
    {
        private static readonly object Gate = new object();

        public static void Append(string operation, params string[] fields)
        {
            lock (Gate)
            {
                try
                {
                    Directory.CreateDirectory(AddinLog.LogDirectory);
                    string file = Path.Combine(
                        AddinLog.LogDirectory,
                        operation + "-" + DateTime.Now.ToString("yyyyMMdd") + ".csv");

                    StringBuilder sb = new StringBuilder();
                    sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    foreach (string field in fields)
                    {
                        sb.Append(',');
                        sb.Append(Escape(field));
                    }

                    File.AppendAllText(file, sb.ToString() + Environment.NewLine, new UTF8Encoding(true));
                }
                catch
                {
                    // Logging must never break the operation.
                }
            }
        }

        private static string Escape(string value)
        {
            string v = value ?? "";
            if (v.IndexOfAny(new char[] { ',', '"', '\r', '\n' }) >= 0)
            {
                return "\"" + v.Replace("\"", "\"\"") + "\"";
            }
            return v;
        }
    }

    //=================================================================================================================================================
    // Nhập EDZ — hỗ trợ scheme (fix S030060)
    //=================================================================================================================================================

    /// <summary>
    /// Gọi MDPartsManagement.ImportParts với field-mapping scheme HỢP LỆ.
    /// EPLAN từ chối scheme rỗng: "S030060:Invalid scheme. Parameter value: ''".
    /// Thứ tự dò: USER.LastUsed → STATION.LastUsed → "Standardvorgabe EPLAN"
    /// → tên scheme đọc từ chính thông báo lỗi.
    /// </summary>
    internal static class ScriptImportSupport
    {
        public const string Converter = "IXPartsImportExportEdz";
        public const string FallbackScheme = "Standardvorgabe EPLAN";

        public static string Import(
            MDPartsManagement management,
            ArrayList files,
            MDPartsManagement.ImportMode mode,
            bool additionalLanguage,
            MDPartsManagement.ImportIdentifier identifier)
        {
            if (management == null) throw new ArgumentNullException("management");

            List<string> candidates = ResolveSchemeCandidates();
            Exception lastSchemeError = null;

            foreach (string scheme in candidates.ToArray())
            {
                try
                {
                    management.ImportParts(files, Converter, mode, additionalLanguage, identifier, scheme);
                    return scheme;
                }
                catch (Exception ex)
                {
                    if (!LooksLikeSchemeError(ex)) throw;

                    lastSchemeError = ex;
                    foreach (string parsed in ParseSchemeNames(ex.Message))
                    {
                        if (!candidates.Contains(parsed)) candidates.Add(parsed);
                    }
                }
            }

            if (lastSchemeError != null) throw lastSchemeError;
            throw new InvalidOperationException("Không tìm được field-mapping scheme để nhập EDZ.");
        }

        public static List<string> ResolveSchemeCandidates()
        {
            List<string> result = new List<string>();

            string[] paths = new string[]
            {
                "USER.PartsManagementGui.FieldMapping.LastUsed",
                "STATION.PartsManagementGui.FieldMapping.LastUsed"
            };

            foreach (string path in paths)
            {
                try
                {
                    Settings settings = new Settings();
                    if (settings.ExistSetting(path))
                    {
                        string value = settings.GetStringSetting(path, 0);
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            value = value.Trim();
                            if (!result.Contains(value)) result.Add(value);
                        }
                    }
                }
                catch
                {
                    // bỏ qua — thử ứng viên kế tiếp
                }
            }

            if (!result.Contains(FallbackScheme)) result.Add(FallbackScheme);
            return result;
        }

        private static bool LooksLikeSchemeError(Exception exception)
        {
            string message = exception == null ? string.Empty : (exception.Message ?? string.Empty);

            if (message.IndexOf("S030060", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (message.IndexOf("Invalid scheme", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (message.IndexOf("scheme", StringComparison.OrdinalIgnoreCase) >= 0 &&
                message.IndexOf('\'') >= 0)
            {
                return true;
            }
            return false;
        }

        private static List<string> ParseSchemeNames(string message)
        {
            List<string> names = new List<string>();
            if (string.IsNullOrEmpty(message)) return names;

            foreach (Match match in Regex.Matches(message, "'([^']+)'"))
            {
                string name = match.Groups[1].Value.Trim();
                if (name.Length > 0 && !names.Contains(name)) names.Add(name);
            }
            return names;
        }
    }

    //=================================================================================================================================================
    // Part model (tree + property reading)
    //=================================================================================================================================================

    /// <summary>
    /// Bọc một MDPart với thông tin hiển thị/định vị cây đã đọc sẵn.
    /// </summary>
    internal sealed class PartEntry
    {
        public readonly MDPart Part;
        public readonly string Number;
        public readonly string Designation;
        public readonly string Manufacturer;
        public readonly string Label;
        public readonly string[] Path;

        private PartEntry(MDPart part, string number, string designation, string manufacturer, string label, string[] path)
        {
            Part = part;
            Number = number;
            Designation = designation;
            Manufacturer = manufacturer;
            Label = label;
            Path = path;
        }

        public static PartEntry FromPart(MDPart part)
        {
            string number = PartInfo.ReadText(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_PARTNR);
            if (number.Length == 0)
            {
                try { number = (part.PartNr ?? string.Empty).Trim(); }
                catch { /* ignore */ }
            }

            string designation = PartInfo.ReadText(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_DESCR1);

            string manufacturer = PartInfo.ReadText(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_MANUFACTURER);
            if (manufacturer.Length == 0)
            {
                manufacturer = PartInfo.ReadText(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_MANUFACTURER_NAME);
            }

            string label = number;
            if (designation.Length > 0 && !string.Equals(designation, number, StringComparison.OrdinalIgnoreCase))
            {
                label = number + " (" + designation + ")";
            }

            List<string> path = new List<string>(4);

            string top = PartInfo.ReadText(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_PRODUCTTOPGROUP);
            path.Add(top.Length > 0 ? top : "(Không nhóm)");

            string group = PartInfo.ReadText(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_PRODUCTGROUP);
            if (group.Length > 0) path.Add(group);

            string sub = PartInfo.ReadText(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_PRODUCTSUBGROUP);
            if (sub.Length > 0) path.Add(sub);

            path.Add(manufacturer.Length > 0 ? manufacturer : "(Không hãng)");

            return new PartEntry(part, number, designation, manufacturer, label, path.ToArray());
        }

        public bool Matches(string query)
        {
            return Number.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                || Designation.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                || Manufacturer.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    /// <summary>
    /// Đọc chuỗi hiển thị của property part — xử lý enum, đa ngôn ngữ và chuỗi thường.
    /// Không bao giờ ném lỗi — trả chuỗi rỗng nếu không đọc được.
    /// </summary>
    internal static class PartInfo
    {
        public static string ReadText(MDPart part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem propertyId)
        {
            try
            {
                MDPropertyValue value = part.Properties[propertyId];
                return ReadDisplayText(value);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ReadDisplayText(MDPropertyValue value)
        {
            if (value == null || value.IsEmpty) return string.Empty;

            // 1) Chuỗi hiển thị theo en_US — hoạt động cho cả enum lẫn đa ngôn ngữ.
            try
            {
                MultiLangString multiLang = value.GetDisplayString();
                if (multiLang != null)
                {
                    string display = Clean(multiLang.GetStringToDisplay(ISOCode.Language.L_en_US));
                    if (display.Length > 0) return display;
                }
            }
            catch
            {
                // thử cách khác
            }

            // 2) ToString theo en_US (đa ngôn ngữ), nhưng enum sẽ trả về số.
            try
            {
                string localized = Clean(value.ToString(ISOCode.Language.L_en_US));
                if (localized.Length > 0 && !IsPureNumber(localized)) return localized;
            }
            catch
            {
                // thử cách khác
            }

            // 3) Chuỗi thô: tách "lang@giá trị;..." và chọn en_US → de_DE → đầu tiên.
            try
            {
                return Clean(ParseRawSegments(value.ToString()));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ParseRawSegments(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;

            string english = null;
            string german = null;
            string first = null;

            foreach (string part in raw.Split(';'))
            {
                string segment = part.Trim();
                if (segment.Length == 0) continue;

                string language;
                string text;
                int at = segment.IndexOf('@');
                if (at < 0)
                {
                    language = string.Empty;
                    text = segment;
                }
                else
                {
                    language = segment.Substring(0, at).Trim();
                    text = segment.Substring(at + 1).Trim();
                }

                if (text.Length == 0) continue;
                if (first == null) first = text;

                if (language.Equals("en_US", StringComparison.OrdinalIgnoreCase) ||
                    language.Equals("en_EN", StringComparison.OrdinalIgnoreCase))
                {
                    english = text;
                    break;
                }

                if (german == null && language.Equals("de_DE", StringComparison.OrdinalIgnoreCase))
                {
                    german = text;
                }
            }

            return english ?? german ?? first ?? string.Empty;
        }

        private static string Clean(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Trim().TrimEnd(';').Trim();
        }

        private static bool IsPureNumber(string value)
        {
            foreach (char c in value)
            {
                if (c < '0' || c > '9') return false;
            }
            return value.Length > 0;
        }
    }

    /// <summary>
    /// Cây nhóm part kiểu navigator EPLAN:
    /// Nhóm sản phẩm (Record type) → nhóm → [phân nhóm] → hãng → part.
    /// </summary>
    internal sealed class PartsTreeModel
    {
        internal sealed class Node
        {
            public readonly string Name;
            public readonly SortedDictionary<string, Node> Children =
                new SortedDictionary<string, Node>(StringComparer.OrdinalIgnoreCase);
            public readonly List<PartEntry> Entries = new List<PartEntry>();
            public int TotalParts;

            public Node(string name)
            {
                Name = name;
            }
        }

        public readonly Node Root = new Node("(root)");
        public int PartCount { get; private set; }

        public static PartsTreeModel Build(IEnumerable<PartEntry> entries)
        {
            PartsTreeModel model = new PartsTreeModel();
            foreach (PartEntry entry in entries)
            {
                Node node = model.Root;
                foreach (string segment in entry.Path)
                {
                    Node child;
                    if (!node.Children.TryGetValue(segment, out child))
                    {
                        child = new Node(segment);
                        node.Children.Add(segment, child);
                    }
                    node = child;
                }
                node.Entries.Add(entry);
                model.PartCount++;
            }

            ComputeTotalsAndSort(model.Root);
            return model;
        }

        private static int ComputeTotalsAndSort(Node node)
        {
            node.Entries.Sort(delegate(PartEntry a, PartEntry b)
            {
                return string.Compare(a.Number, b.Number, StringComparison.OrdinalIgnoreCase);
            });
            int total = node.Entries.Count;
            foreach (Node child in node.Children.Values)
            {
                total += ComputeTotalsAndSort(child);
            }
            node.TotalParts = total;
            return total;
        }

        /// <summary>Dòng mô tả cho log chẩn đoán (số part theo nhóm cấp cao nhất).</summary>
        public IEnumerable<string> DescribeTopGroups(int max)
        {
            int count = 0;
            foreach (Node child in Root.Children.Values)
            {
                if (count++ >= max) yield break;
                yield return "  " + child.Name + ": " + child.TotalParts + " part";
            }
        }
    }

    //=================================================================================================================================================
    // Nhập EDZ
    //=================================================================================================================================================

    /// <summary>
    /// Nhập batch gói EDZ vào Parts Database (MDPartsManagement.ImportParts).
    ///
    /// Giao diện đơn giản: chọn file/thư mục → NHẬP.
    /// Các tùy chọn kỹ thuật được cố định ở hậu trường:
    ///   - Chế độ nhập      : Cập nhật + thêm mới (UpdateAndAppend)
    ///   - Nhận dạng part   : Theo mã hàng (PartNumber)
    ///   - Ngôn ngữ phụ     : không
    ///   - Field-mapping scheme: tự dò (xem ScriptImportSupport)
    /// </summary>
    internal sealed class ImportEdzForm : Form
    {
        private const MDPartsManagement.ImportMode FixedImportMode = MDPartsManagement.ImportMode.UpdateAndAppend;
        private const MDPartsManagement.ImportIdentifier FixedIdentifier = MDPartsManagement.ImportIdentifier.PartNumber;
        private const bool FixedAdditionalLanguage = false;

        private readonly ListBox _fileList = new ListBox();
        private readonly TextBox _log = new TextBox();
        private readonly Button _startButton = new Button();

        public ImportEdzForm()
        {
            Text = "Tool Oumijia — Nhập EDZ vào Parts Database";
            ClientSize = new Size(880, 620);
            MinimumSize = new Size(700, 480);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);
            BuildUi();
        }

        private void BuildUi()
        {
            // ----- trên: tiêu đề + nút thêm file + ghi chú mặc định
            Panel topPanel = new Panel { Dock = DockStyle.Top, Height = 94 };

            Label label = new Label
            {
                Text = "Các gói EDZ cần nhập:",
                Dock = DockStyle.Top,
                Height = 24,
                Padding = new Padding(4, 4, 0, 0)
            };

            FlowLayoutPanel buttonFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 34,
                WrapContents = false,
                Padding = new Padding(4, 2, 0, 0)
            };

            Button btnAddFiles = new Button { Text = "Thêm tệp…", Width = 96, Height = 26 };
            btnAddFiles.Click += OnAddFilesClicked;
            Button btnAddFolder = new Button { Text = "Thêm thư mục…", Width = 116, Height = 26 };
            btnAddFolder.Click += OnAddFolderClicked;
            Button btnRemove = new Button { Text = "Xóa mục chọn", Width = 100, Height = 26 };
            btnRemove.Click += delegate
            {
                foreach (object item in _fileList.SelectedItems.Cast<object>().ToList())
                {
                    _fileList.Items.Remove(item);
                }
            };
            Button btnClear = new Button { Text = "Xóa hết", Width = 80, Height = 26 };
            btnClear.Click += delegate { _fileList.Items.Clear(); };

            buttonFlow.Controls.Add(btnAddFiles);
            buttonFlow.Controls.Add(btnAddFolder);
            buttonFlow.Controls.Add(btnRemove);
            buttonFlow.Controls.Add(btnClear);

            Label hint = new Label
            {
                Dock = DockStyle.Top,
                Height = 32,
                Padding = new Padding(4, 2, 0, 0),
                ForeColor = Color.DimGray,
                Text = "Mặc định khi nhập: cập nhật part đã có + thêm part mới (nhận dạng theo mã hàng).\r\n" +
                       "Khuyến nghị: thử trên database test trước khi nhập vào database chính."
            };

            topPanel.Controls.Add(hint);
            topPanel.Controls.Add(buttonFlow);
            topPanel.Controls.Add(label);

            // ----- đáy: nút nhập + log
            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 220 };

            FlowLayoutPanel actionFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 44,
                WrapContents = false,
                Padding = new Padding(4, 6, 0, 0)
            };

            _startButton.Text = "NHẬP VÀO PARTS DATABASE ▸";
            _startButton.Size = new Size(220, 30);
            _startButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _startButton.Click += OnStartClicked;
            actionFlow.Controls.Add(_startButton);

            _log.Dock = DockStyle.Fill;
            _log.Multiline = true;
            _log.ReadOnly = true;
            _log.ScrollBars = ScrollBars.Both;
            _log.WordWrap = false;
            _log.Font = new Font("Consolas", 8.5F);

            bottomPanel.Controls.Add(_log);
            bottomPanel.Controls.Add(actionFlow);

            _fileList.Dock = DockStyle.Fill;
            _fileList.SelectionMode = SelectionMode.MultiExtended;

            Controls.Add(_fileList);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);
        }

        private void OnAddFilesClicked(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Chọn gói EDZ";
                dialog.Filter = "Gói EPLAN (*.edz)|*.edz|Tất cả tệp (*.*)|*.*";
                dialog.Multiselect = true;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                AddDistinct(dialog.FileNames);
            }
        }

        private void OnAddFolderClicked(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Chọn thư mục chứa EDZ (bao gồm thư mục con)";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    AddDistinct(Directory.EnumerateFiles(dialog.SelectedPath, "*.edz", SearchOption.AllDirectories));
                }
                catch (Exception ex)
                {
                    Log("LỖI đọc thư mục: " + ex.Message);
                }
            }
        }

        private void AddDistinct(IEnumerable<string> files)
        {
            HashSet<string> existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (object item in _fileList.Items)
            {
                if (item != null) existing.Add(item.ToString());
            }

            int added = 0;
            foreach (string file in files)
            {
                if (string.IsNullOrWhiteSpace(file)) continue;
                if (existing.Add(file))
                {
                    _fileList.Items.Add(file);
                    added++;
                }
            }

            Log("Đã thêm " + added + " gói EDZ (tổng: " + _fileList.Items.Count + ").");
        }

        private void OnStartClicked(object sender, EventArgs e)
        {
            if (_fileList.Items.Count == 0)
            {
                MessageBox.Show(this, "Chưa có gói EDZ nào trong danh sách.", "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult confirm = MessageBox.Show(this,
                "Nhập " + _fileList.Items.Count + " gói EDZ vào Parts Database?\n\n" +
                "Mặc định: cập nhật part đã có + thêm part mới (theo mã hàng).\n\n" +
                "Bạn nên kiểm tra trên database test trước khi nhập vào database chính.",
                "Tool Oumijia", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            _startButton.Enabled = false;
            try
            {
                ArrayList files = new ArrayList();
                foreach (object item in _fileList.Items) files.Add(item.ToString());

                Log("Bắt đầu nhập " + files.Count + " gói (chế độ cập nhật + thêm mới, theo mã hàng) …");
                AddinLog.Write("Import: " + files.Count + " files, mode=UpdateAndAppend, identifier=PartNumber");

                MDPartsManagement management = new MDPartsManagement();
                string scheme = ScriptImportSupport.Import(management, files,
                    FixedImportMode, FixedAdditionalLanguage, FixedIdentifier);

                Log("EPLAN đã xử lý xong yêu cầu nhập (scheme: '" + scheme + "').");
                AddinLog.Write("Import finished OK (scheme='" + scheme + "')");
                AddinCsvLog.Append("import", "OK", FixedImportMode.ToString(), FixedIdentifier.ToString(),
                    files.Count.ToString(), scheme);

                MessageBox.Show(this,
                    "Đã gửi yêu cầu nhập " + files.Count + " gói cho EPLAN.\n" +
                    "Kiểm tra Parts Management để xác nhận kết quả.",
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log("LỖI: " + ex.Message);
                AddinLog.WriteError("Import", ex);
                AddinCsvLog.Append("import", "ERROR", FixedImportMode.ToString(), ex.Message);
                MessageBox.Show(this, "Nhập thất bại:\n" + ex.Message, "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _startButton.Enabled = true;
            }
        }

        private void Log(string message)
        {
            try
            {
                _log.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + message + Environment.NewLine);
            }
            catch
            {
                // Không bao giờ để log làm hỏng thao tác.
            }
        }
    }

    //=================================================================================================================================================
    // Xuất EDZ
    //=================================================================================================================================================

    /// <summary>
    /// Xuất EDZ:
    ///  - Cây nhóm kiểu navigator EPLAN (tick 3 trạng thái, dòng đã chọn in đậm, tìm kiếm)
    ///  - Panel CHI TIẾT bên phải (thuộc tính part kiểu EPLAN Overview)
    ///  - Hai phạm vi xuất: "Part đã chọn trên cây" hoặc "Theo loại bản ghi (như EPLAN)"
    ///  - "Một file EDZ": chỉ chọn THƯ MỤC; tên tự động theo mã sản phẩm / parts.edz
    /// </summary>
    internal sealed class ExportEdzForm : Form
    {
        private const string PartsDefaultFileName = "parts.edz";

        private readonly Font _boldFont = new Font("Segoe UI", 9F, FontStyle.Bold);
        private bool _updatingChecks;

        // ----- hàng trên
        private readonly Label _dbLabel = new Label();
        private readonly TextBox _searchBox = new TextBox();
        private readonly Button _btnSelectAll = new Button();
        private readonly Button _btnClear = new Button();
        private readonly Button _btnFromPartsMgmt = new Button();
        private readonly Label _hintLabel = new Label();

        // ----- giữa: cây + chi tiết
        private readonly TreeView _tree = new TreeView();
        private readonly Label _detailsHeader = new Label();
        private readonly ListView _details = new ListView();
        private SplitContainer _split;

        // ----- tùy chọn xuất
        private readonly GroupBox _outputGroup = new GroupBox();
        private readonly RadioButton _rbTree = new RadioButton();
        private readonly RadioButton _rbRecords = new RadioButton();
        private readonly FlowLayoutPanel _recordsPanel = new FlowLayoutPanel();
        private readonly CheckBox _cbParts = new CheckBox();
        private readonly CheckBox _cbAccessoryLists = new CheckBox();
        private readonly CheckBox _cbAccessoryPlacements = new CheckBox();
        private readonly CheckBox _cbDrilling = new CheckBox();
        private readonly CheckBox _cbConnectionPoints = new CheckBox();
        private readonly CheckBox _cbAddresses = new CheckBox();
        private readonly Label _recordsHint = new Label();
        private readonly RadioButton _rbSingle = new RadioButton();
        private readonly TextBox _fileTarget = new TextBox();
        private readonly Button _browseFile = new Button();
        private readonly RadioButton _rbPerPart = new RadioButton();
        private readonly TextBox _folderTarget = new TextBox();
        private readonly Button _browseFolder = new Button();
        private readonly Label _targetPreview = new Label();

        // ----- đáy
        private readonly Label _countLabel = new Label();
        private readonly Button _exportButton = new Button();
        private readonly TextBox _log = new TextBox();

        private readonly Timer _searchTimer = new Timer();

        private MDPartsManagement _management;
        private MDPartsDatabase _database;
        private List<PartEntry> _entries = new List<PartEntry>();
        private readonly HashSet<int> _checkedIds = new HashSet<int>();
        private string _searchText = string.Empty;

        public ExportEdzForm()
        {
            Text = "Tool Oumijia — Xuất EDZ (chọn part cần xuất)";
            ClientSize = new Size(1080, 780);
            MinimumSize = new Size(940, 640);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);
            BuildUi();
        }

        // ================================================================ UI

        private void BuildUi()
        {
            // ----- hàng trên
            Panel topPanel = new Panel { Dock = DockStyle.Top, Height = 92 };

            _dbLabel.Dock = DockStyle.Top;
            _dbLabel.Height = 24;
            _dbLabel.Padding = new Padding(4, 4, 0, 0);
            _dbLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            FlowLayoutPanel searchRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 34,
                WrapContents = false,
                Padding = new Padding(4, 2, 0, 0)
            };

            Label lblSearch = new Label { Text = "Tìm:", AutoSize = true, Margin = new Padding(0, 7, 4, 0) };
            _searchBox.Width = 240;
            _searchBox.Margin = new Padding(0, 4, 8, 0);

            _btnSelectAll.Text = "Chọn tất cả";
            _btnSelectAll.Size = new Size(96, 26);
            _btnSelectAll.Margin = new Padding(0, 3, 4, 0);
            _btnSelectAll.Click += OnSelectAllClicked;

            _btnClear.Text = "Bỏ chọn";
            _btnClear.Size = new Size(80, 26);
            _btnClear.Margin = new Padding(0, 3, 4, 0);
            _btnClear.Click += OnClearAllClicked;

            _btnFromPartsMgmt.Text = "Lấy từ Parts Management ▸";
            _btnFromPartsMgmt.Size = new Size(190, 26);
            _btnFromPartsMgmt.Margin = new Padding(0, 3, 0, 0);
            _btnFromPartsMgmt.Click += OnUseSelectionClicked;

            searchRow.Controls.Add(lblSearch);
            searchRow.Controls.Add(_searchBox);
            searchRow.Controls.Add(_btnSelectAll);
            searchRow.Controls.Add(_btnClear);
            searchRow.Controls.Add(_btnFromPartsMgmt);

            _hintLabel.Dock = DockStyle.Top;
            _hintLabel.Height = 22;
            _hintLabel.Padding = new Padding(4, 0, 0, 0);
            _hintLabel.ForeColor = Color.DimGray;
            _hintLabel.Text = "Mẹo: tick ô vuông trước mỗi mục để chọn; tick nhóm = chọn cả nhánh · bấm dòng để xem chi tiết.";

            topPanel.Controls.Add(_hintLabel);
            topPanel.Controls.Add(searchRow);
            topPanel.Controls.Add(_dbLabel);

            // ----- giữa: cây | chi tiết
            _tree.Dock = DockStyle.Fill;
            _tree.BorderStyle = BorderStyle.FixedSingle;
            _tree.HideSelection = false;
            _tree.ShowLines = true;
            _tree.ShowRootLines = true;
            _tree.ItemHeight = 22;
            _tree.CheckBoxes = true;
            _tree.AfterCheck += OnTreeAfterCheck;
            _tree.AfterSelect += delegate(object s, TreeViewEventArgs e) { ShowDetails(e.Node); };
            EnableDoubleBuffering(_tree);

            _detailsHeader.Dock = DockStyle.Top;
            _detailsHeader.Height = 24;
            _detailsHeader.Padding = new Padding(4, 3, 0, 0);
            _detailsHeader.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _detailsHeader.Text = "Chi tiết";

            _details.Dock = DockStyle.Fill;
            _details.View = View.Details;
            _details.FullRowSelect = true;
            _details.MultiSelect = false;
            _details.HideSelection = false;
            _details.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            _details.Columns.Add("Thuộc tính", 190);
            _details.Columns.Add("Giá trị", 320);
            _details.DoubleClick += OnDetailsDoubleClick;

            _split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical
            };
            // Panel1MinSize/Panel2MinSize và SplitterDistance chỉ đặt được khi
            // SplitContainer đã có kích thước thật — sẽ áp dụng trong OnShown.
            _split.Panel1.Controls.Add(_tree);
            _split.Panel2.Controls.Add(_details);
            _split.Panel2.Controls.Add(_detailsHeader);
            _split.Panel2.Resize += delegate { AdjustDetailsColumns(); };
            _split.SplitterMoved += delegate { AdjustDetailsColumns(); };

            // ----- đáy: tuỳ chọn + đếm + log
            Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 330 };

            BuildOutputGroup();

            Panel actionPanel = new Panel { Dock = DockStyle.Top, Height = 42 };
            TableLayoutPanel actionLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            _countLabel.Text = "Đã chọn: 0 part";
            _countLabel.AutoSize = true;
            _countLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _countLabel.Anchor = AnchorStyles.Left;
            _countLabel.Margin = new Padding(4, 10, 0, 0);

            _exportButton.Text = "XUẤT RA EDZ ▸";
            _exportButton.Size = new Size(160, 32);
            _exportButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _exportButton.Enabled = false;
            _exportButton.Dock = DockStyle.Right;
            _exportButton.Click += OnExportClicked;

            actionLayout.Controls.Add(_countLabel, 0, 0);
            actionLayout.Controls.Add(_exportButton, 1, 0);
            actionPanel.Controls.Add(actionLayout);

            _log.Dock = DockStyle.Fill;
            _log.Multiline = true;
            _log.ReadOnly = true;
            _log.ScrollBars = ScrollBars.Both;
            _log.WordWrap = false;
            _log.Font = new Font("Consolas", 8.5F);

            bottomPanel.Controls.Add(_log);
            bottomPanel.Controls.Add(actionPanel);
            bottomPanel.Controls.Add(_outputGroup);

            Controls.Add(_split);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);

            // ----- tìm kiếm (debounce)
            _searchTimer.Interval = 250;
            _searchTimer.Tick += delegate
            {
                _searchTimer.Stop();
                string text = (_searchBox.Text ?? string.Empty).Trim();
                if (text == _searchText) return;
                _searchText = text;
                RebuildTree();
            };
            _searchBox.TextChanged += delegate
            {
                _searchTimer.Stop();
                _searchTimer.Start();
            };
        }

        private void BuildOutputGroup()
        {
            _outputGroup.Dock = DockStyle.Top;
            _outputGroup.Height = 158;
            _outputGroup.Text = "Cách xuất";

            // ----- hàng "phạm vi"
            FlowLayoutPanel scopeFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 28,
                WrapContents = false,
                Padding = new Padding(6, 2, 0, 0)
            };

            Label lblScope = new Label { Text = "Phạm vi:", AutoSize = true, Margin = new Padding(0, 5, 4, 0) };
            _rbTree.Text = "Part đã chọn trên cây";
            _rbTree.AutoSize = true;
            _rbTree.Checked = true;
            _rbTree.Margin = new Padding(0, 3, 12, 0);
            _rbTree.CheckedChanged += delegate { UpdateModes(); };
            _rbRecords.Text = "Theo loại bản ghi (như EPLAN)";
            _rbRecords.AutoSize = true;
            _rbRecords.Margin = new Padding(0, 3, 0, 0);
            _rbRecords.CheckedChanged += delegate { UpdateModes(); };

            scopeFlow.Controls.Add(lblScope);
            scopeFlow.Controls.Add(_rbTree);
            scopeFlow.Controls.Add(_rbRecords);

            // ----- hàng checkbox loại bản ghi
            _recordsPanel.Dock = DockStyle.Top;
            _recordsPanel.Height = 58;
            _recordsPanel.WrapContents = true;
            _recordsPanel.Padding = new Padding(20, 0, 0, 0);
            _recordsPanel.Visible = false;

            SetupRecordCheckBox(_cbParts, "Parts (linh kiện)", true);
            SetupRecordCheckBox(_cbAccessoryLists, "Accessory list (danh mục phụ kiện)", false);
            SetupRecordCheckBox(_cbAccessoryPlacements, "Accessory placement (vị trí phụ kiện)", false);
            SetupRecordCheckBox(_cbDrilling, "Drilling pattern (mẫu khoan)", false);
            SetupRecordCheckBox(_cbConnectionPoints, "Connection point pattern (mẫu điểm nối)", false);
            SetupRecordCheckBox(_cbAddresses, "Customer / Manufacturer / Supplier (KH & hãng)", false);

            _recordsHint.Dock = DockStyle.Top;
            _recordsHint.Height = 20;
            _recordsHint.Padding = new Padding(20, 0, 0, 0);
            _recordsHint.ForeColor = Color.DimGray;
            _recordsHint.Visible = false;
            _recordsHint.Text = "Chế độ này xuất TOÀN BỘ bản ghi theo loại đã tick (không dùng tick trên cây); file kèm macro/tài liệu nên có thể lớn.";

            // ----- hàng đích xuất (bảng 3 cột × 3 hàng, hàng 3 = xem trước tên file)
            TableLayoutPanel targetLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 90,
                ColumnCount = 3,
                RowCount = 3,
                Padding = new Padding(6, 0, 6, 0)
            };
            targetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            targetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            targetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            targetLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            targetLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            targetLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));

            _rbSingle.Text = "Một file EDZ (vào thư mục):";
            _rbSingle.AutoSize = true;
            _rbSingle.Checked = true;
            _rbSingle.Anchor = AnchorStyles.Left;
            _rbSingle.Margin = new Padding(0, 4, 8, 0);
            _rbSingle.CheckedChanged += delegate { UpdateTargetPreview(); };

            _fileTarget.Dock = DockStyle.Fill;
            _fileTarget.Margin = new Padding(0, 3, 6, 3);

            _browseFile.Text = "Chọn thư mục…";
            _browseFile.Size = new Size(116, 26);
            _browseFile.Anchor = AnchorStyles.Left;
            _browseFile.Margin = new Padding(0, 2, 0, 0);
            _browseFile.Click += OnBrowseFileClicked;

            _rbPerPart.Text = "Mỗi part một file (vào thư mục):";
            _rbPerPart.AutoSize = true;
            _rbPerPart.Anchor = AnchorStyles.Left;
            _rbPerPart.Margin = new Padding(0, 4, 8, 0);
            _rbPerPart.CheckedChanged += delegate { UpdateTargetPreview(); };

            _folderTarget.Dock = DockStyle.Fill;
            _folderTarget.Margin = new Padding(0, 3, 6, 3);

            _browseFolder.Text = "Chọn thư mục…";
            _browseFolder.Size = new Size(116, 26);
            _browseFolder.Anchor = AnchorStyles.Left;
            _browseFolder.Margin = new Padding(0, 2, 0, 0);
            _browseFolder.Click += OnBrowseFolderClicked;

            _targetPreview.Dock = DockStyle.Fill;
            _targetPreview.ForeColor = Color.DimGray;
            _targetPreview.Margin = new Padding(0, 4, 0, 0);
            _targetPreview.TextAlign = ContentAlignment.MiddleLeft;
            _targetPreview.Text = "→ sẽ tạo: …";

            targetLayout.Controls.Add(_rbSingle, 0, 0);
            targetLayout.Controls.Add(_fileTarget, 1, 0);
            targetLayout.Controls.Add(_browseFile, 2, 0);
            targetLayout.Controls.Add(_rbPerPart, 0, 1);
            targetLayout.Controls.Add(_folderTarget, 1, 1);
            targetLayout.Controls.Add(_browseFolder, 2, 1);
            targetLayout.Controls.Add(_targetPreview, 0, 2);
            targetLayout.SetColumnSpan(_targetPreview, 3);

            // Dock=Top: control thêm SAU nằm TRÊN → thêm ngược thứ tự hiển thị.
            _outputGroup.Controls.Add(targetLayout);
            _outputGroup.Controls.Add(_recordsHint);
            _outputGroup.Controls.Add(_recordsPanel);
            _outputGroup.Controls.Add(scopeFlow);
        }

        private void SetupRecordCheckBox(CheckBox box, string text, bool isChecked)
        {
            box.Text = text;
            box.AutoSize = true;
            box.Checked = isChecked;
            box.Margin = new Padding(0, 2, 16, 0);
            box.CheckedChanged += delegate { UpdateCount(); };
            _recordsPanel.Controls.Add(box);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _boldFont.Dispose();
                _searchTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        private static void EnableDoubleBuffering(Control control)
        {
            try
            {
                typeof(Control)
                    .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(control, true, null);
            }
            catch
            {
                // Chỉ là tối ưu vẽ.
            }
        }

        private void AdjustDetailsColumns()
        {
            try
            {
                if (_details.Columns.Count >= 2)
                {
                    _details.Columns[1].Width = Math.Max(120, _details.ClientSize.Width - _details.Columns[0].Width - 26);
                }
            }
            catch
            {
                // bỏ qua
            }
        }

        // ================================================================ nạp dữ liệu

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Áp dụng kích thước splitter sau khi form đã có kích thước thật.
            try
            {
                _split.Panel1MinSize = 380;
                _split.Panel2MinSize = 250;
            }
            catch
            {
                // giữ mặc định
            }
            try
            {
                _split.SplitterDistance = Math.Max(400, (int)(_split.Width * 0.58));
            }
            catch
            {
                // giữ mặc định
            }

            AdjustDetailsColumns();

            LoadLastFolders();
            UpdateTargetPreview();

            LoadDatabase();
        }

        private void LoadDatabase()
        {
            try
            {
                _management = new MDPartsManagement();
                _database = _management.SelectedPartsDatabase;
            }
            catch (Exception ex)
            {
                AddinLog.Write("Export: đọc SelectedPartsDatabase lỗi: " + ex.Message);
            }

            if (_database == null)
            {
                MessageBox.Show(this,
                    "Chưa có Parts Database nào đang mở trong phiên EPLAN này.\n\n" +
                    "Hãy mở: Master data → Parts → Management (chọn database) rồi thực hiện lại.",
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Close();
                return;
            }

            UseWaitCursor = true;
            string dbName = SafeDatabaseName();
            _dbLabel.Text = "Database: " + dbName + " — đang tải danh sách part…";
            try
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                List<MDPart> parts = new List<MDPart>(_database.Parts);
                long enumMs = stopwatch.ElapsedMilliseconds;

                stopwatch.Restart();
                _entries = new List<PartEntry>(parts.Count);
                foreach (MDPart part in parts)
                {
                    _entries.Add(PartEntry.FromPart(part));
                }
                stopwatch.Stop();

                AddinLog.Write("Export: nạp " + parts.Count + " part (enum=" + enumMs +
                               " ms, đọc property=" + stopwatch.ElapsedMilliseconds + " ms)");

                _dbLabel.Text = "Database: " + dbName + " — " + parts.Count + " part";
                RebuildTree();
                Log("Đã nạp " + parts.Count + " part từ database '" + dbName + "'.");
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("Export: nạp danh sách part", ex);
                MessageBox.Show(this, "Không đọc được danh sách part:\n" + ex.Message,
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private string SafeDatabaseName()
        {
            try
            {
                string raw = MDPartsManagement.SelectedPartsDatabaseAsString;
                if (string.IsNullOrWhiteSpace(raw)) return "(không rõ)";
                // "$(MD_PARTS)\ESS_part001.alk" → "ESS_part001.alk"
                try
                {
                    string fileName = Path.GetFileName(raw.Trim());
                    if (!string.IsNullOrWhiteSpace(fileName)) return fileName;
                }
                catch
                {
                    // giữ nguyên chuỗi gốc
                }
                return raw.Trim();
            }
            catch
            {
                return "(không rõ)";
            }
        }

        // ================================================================ cây

        private void RebuildTree()
        {
            List<PartEntry> filtered = string.IsNullOrEmpty(_searchText)
                ? _entries
                : _entries.Where(delegate(PartEntry x) { return x.Matches(_searchText); }).ToList();

            PartsTreeModel model = PartsTreeModel.Build(filtered);

            _tree.BeginUpdate();
            try
            {
                _tree.Nodes.Clear();
                foreach (PartsTreeModel.Node child in model.Root.Children.Values)
                {
                    _tree.Nodes.Add(CreateTreeNode(child));
                }
            }
            finally
            {
                _tree.EndUpdate();
            }

            if (_searchText.Length > 0)
            {
                _tree.ExpandAll();
            }
            else
            {
                foreach (TreeNode top in _tree.Nodes)
                {
                    top.Expand();
                }
            }

            UpdateCount();
        }

        private TreeNode CreateTreeNode(PartsTreeModel.Node node)
        {
            TreeNode treeNode = new TreeNode(node.Name);
            treeNode.Tag = node;

            foreach (PartEntry entry in node.Entries)
            {
                TreeNode leaf = new TreeNode(entry.Label);
                leaf.Tag = entry;
                treeNode.Nodes.Add(leaf);
            }
            foreach (PartsTreeModel.Node child in node.Children.Values)
            {
                treeNode.Nodes.Add(CreateTreeNode(child));
            }

            RefreshNodeState(treeNode);
            return treeNode;
        }

        /// <summary>Đồng bộ checkbox và in đậm sau khi cấu hình/tạo cây.</summary>
        private void RefreshNodeState(TreeNode node)
        {
            _updatingChecks = true;
            try
            {
                ApplyCheckedStateToSubtree(node);
            }
            finally
            {
                _updatingChecks = false;
            }
        }

        private void ApplyCheckedStateToSubtree(TreeNode node)
        {
            PartEntry entry = node.Tag as PartEntry;
            if (entry != null)
            {
                node.Checked = _checkedIds.Contains(entry.Part.AdoId);
                node.NodeFont = node.Checked ? _boldFont : null;
                return;
            }

            int checkedCount = 0;
            foreach (TreeNode child in node.Nodes)
            {
                ApplyCheckedStateToSubtree(child);
                if (child.Checked) checkedCount++;
            }

            // Windows checkbox không có Mixed; nhánh chỉ tick khi đủ toàn bộ con.
            node.Checked = node.Nodes.Count > 0 && checkedCount == node.Nodes.Count;
            node.NodeFont = null;
        }

        private void RefreshTreeStatesDeep(TreeNode node)
        {
            RefreshNodeState(node);
        }

        private void RefreshAncestors(TreeNode node)
        {
            TreeNode parent = node.Parent;
            while (parent != null)
            {
                _updatingChecks = true;
                try
                {
                    int checkedCount = 0;
                    foreach (TreeNode child in parent.Nodes)
                    {
                        if (child.Checked) checkedCount++;
                    }
                    parent.Checked = parent.Nodes.Count > 0 && checkedCount == parent.Nodes.Count;
                    parent.NodeFont = null;
                }
                finally
                {
                    _updatingChecks = false;
                }
                parent = parent.Parent;
            }
        }

        private void SetCheckedForSubtree(TreeNode node, bool value)
        {
            _updatingChecks = true;
            try
            {
                ApplyCheckedValue(node, value);
            }
            finally
            {
                _updatingChecks = false;
            }

            RefreshAncestors(node);
        }

        private void ApplyCheckedValue(TreeNode node, bool value)
        {
            PartEntry entry = node.Tag as PartEntry;
            if (entry != null)
            {
                if (value) _checkedIds.Add(entry.Part.AdoId);
                else _checkedIds.Remove(entry.Part.AdoId);
            }

            node.Checked = value;
            node.NodeFont = value && entry != null ? _boldFont : null;

            foreach (TreeNode child in node.Nodes)
            {
                ApplyCheckedValue(child, value);
            }
        }

        private void OnTreeAfterCheck(object sender, TreeViewEventArgs e)
        {
            if (_updatingChecks) return;

            TreeNode node = e.Node;
            bool isChecked = node.Checked;

            // Ở chế độ theo loại bản ghi, cây không quyết định xuất — hoàn tác.
            if (_rbRecords.Checked)
            {
                _updatingChecks = true;
                try
                {
                    node.Checked = !isChecked;
                }
                finally
                {
                    _updatingChecks = false;
                }

                MessageBox.Show(this,
                    "Đang ở chế độ \"Theo loại bản ghi\" — tick trên cây không được dùng.\n" +
                    "Chuyển về \"Part đã chọn trên cây\" nếu muốn chọn từng part.",
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetCheckedForSubtree(node, isChecked);
            UpdateCount();
            ShowDetails(_tree.SelectedNode);
        }

        private void OnSelectAllClicked(object sender, EventArgs e)
        {
            if (_rbRecords.Checked) return;
            foreach (TreeNode top in _tree.Nodes)
            {
                SetCheckedForSubtree(top, true);
            }
            UpdateCount();
            ShowDetails(_tree.SelectedNode);
            Log("Đã chọn tất cả part đang hiển thị.");
        }

        private void OnClearAllClicked(object sender, EventArgs e)
        {
            if (_rbRecords.Checked) return;
            _checkedIds.Clear();
            foreach (TreeNode top in _tree.Nodes)
            {
                RefreshTreeStatesDeep(top);
            }
            UpdateCount();
            ShowDetails(_tree.SelectedNode);
            Log("Đã bỏ chọn tất cả.");
        }

        private void OnUseSelectionClicked(object sender, EventArgs e)
        {
            if (_management == null || _rbRecords.Checked) return;

            int added = 0;
            try
            {
                foreach (object item in _management.GetSelectedPartDatabaseItems(false))
                {
                    MDPart part = item as MDPart;
                    if (part == null) continue;
                    if (_checkedIds.Add(part.AdoId)) added++;
                }
            }
            catch (Exception ex)
            {
                Log("Không đọc được danh sách đang chọn trong Parts Management: " + ex.Message);
            }

            foreach (TreeNode top in _tree.Nodes)
            {
                RefreshTreeStatesDeep(top);
            }
            UpdateCount();
            ShowDetails(_tree.SelectedNode);
            Log("Lấy " + added + " part từ Parts Management (tổng đã chọn: " + _checkedIds.Count + ").");
        }

        // ================================================================ chi tiết

        private void ShowDetails(TreeNode node)
        {
            _details.BeginUpdate();
            try
            {
                _details.Items.Clear();

                if (node == null)
                {
                    _detailsHeader.Text = "Chi tiết";
                    return;
                }

                PartEntry entry = node.Tag as PartEntry;
                if (entry != null)
                {
                    MDPart part = entry.Part;
                    _detailsHeader.Text = entry.Label;

                    AddDetail("Mã hàng (Part number)", entry.Number);
                    AddDetail("Số bản (Type number)", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_TYPENR));
                    AddDetail("Số đặt hàng (Order number)", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_ORDERNR));
                    AddDetail("ERP number", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_ERPNR));

                    string manufacturerName = ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_MANUFACTURER_NAME);
                    AddDetail("Hãng (Manufacturer)",
                        manufacturerName.Length > 0 && !string.Equals(manufacturerName, entry.Manufacturer, StringComparison.OrdinalIgnoreCase)
                            ? entry.Manufacturer + " (" + manufacturerName + ")"
                            : entry.Manufacturer);

                    string supplier = ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_SUPPLIER);
                    string supplierName = ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_SUPPLIER_NAME);
                    AddDetail("Nhà cung cấp (Supplier)",
                        supplier.Length == 0
                            ? supplierName
                            : (supplierName.Length > 0 && !string.Equals(supplierName, supplier, StringComparison.OrdinalIgnoreCase)
                                ? supplier + " (" + supplierName + ")"
                                : supplier));

                    string groupPath = entry.Path.Length > 1
                        ? string.Join(" / ", entry.Path.Take(entry.Path.Length - 1))
                        : string.Empty;
                    AddDetail("Nhóm sản phẩm", groupPath);

                    AddDetail("Tên 1 (Designation 1)", entry.Designation);
                    AddDetail("Tên 2 (Designation 2)", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_DESCR2));
                    AddDetail("Tên 3 (Designation 3)", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_DESCR3));
                    AddDetail("Mô tả (Description)", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_NOTE));
                    AddDetail("Văn bản bổ sung", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_ADDITIONALTEXT));
                    AddDetail("eCl@ss", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.PART_ECLASS));
                    AddDetail("Mã nhận dạng (EAN)",
                        (ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_IDENTTYPE) + " " +
                         ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_IDENTCODE)).Trim());
                    AddDetail("Macro ký hiệu (schematic)", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_GROUPSYMBOLMACRO));
                    AddDetail("Macro đồ họa (graphical)", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_MACRO));
                    AddDetail("Kích thước R×C×S (mm)", ComposeDimensions(part));
                    AddDetail("Ngày tạo", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.PART_CREATE_DATE));
                    AddDetail("Người tạo", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.PART_CREATE_USER));
                    AddDetail("Sửa đổi cuối", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.PART_LASTCHANGE_DATE));
                    AddDetail("Người sửa cuối", ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.PART_LASTCHANGE_USER));
                }
                else
                {
                    PartsTreeModel.Node group = node.Tag as PartsTreeModel.Node;
                    if (group != null)
                    {
                        _detailsHeader.Text = "Nhóm: " + group.Name;
                        AddDetail("Số part trong nhóm", group.TotalParts.ToString());
                        AddDetail("Đã chọn trong nhóm", CountCheckedInSubtree(node).ToString());
                        AddDetail("Gợi ý", "Tick ô vuông trước nhóm để chọn/bỏ chọn cả nhánh.");
                    }
                    else
                    {
                        _detailsHeader.Text = node.Text ?? "Chi tiết";
                    }
                }
            }
            finally
            {
                _details.EndUpdate();
            }
        }

        private static string ReadProp(MDPart part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem id)
        {
            return PartInfo.ReadText(part, id);
        }

        private static string ComposeDimensions(MDPart part)
        {
            string width = ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_WIDTH);
            string height = ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_HEIGHT);
            string depth = ReadProp(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_DEPTH);
            List<string> parts = new List<string>();
            if (width.Length > 0) parts.Add(width);
            if (height.Length > 0) parts.Add(height);
            if (depth.Length > 0) parts.Add(depth);
            return string.Join(" × ", parts);
        }

        private void AddDetail(string name, string value)
        {
            ListViewItem item = new ListViewItem(name ?? string.Empty);
            item.SubItems.Add(string.IsNullOrWhiteSpace(value) ? "—" : value);
            _details.Items.Add(item);
        }

        private int CountCheckedInSubtree(TreeNode node)
        {
            PartEntry entry = node.Tag as PartEntry;
            if (entry != null)
            {
                return _checkedIds.Contains(entry.Part.AdoId) ? 1 : 0;
            }

            int count = 0;
            foreach (TreeNode child in node.Nodes)
            {
                count += CountCheckedInSubtree(child);
            }
            return count;
        }

        private void OnDetailsDoubleClick(object sender, EventArgs e)
        {
            if (_details.SelectedItems.Count == 0) return;
            ListViewItem item = _details.SelectedItems[0];
            if (item.SubItems.Count < 2) return;

            string value = item.SubItems[1].Text;
            if (string.IsNullOrEmpty(value) || value == "—") return;

            try
            {
                Clipboard.SetText(value);
                Log("Đã copy vào clipboard: " + value);
            }
            catch (Exception ex)
            {
                Log("Không copy được: " + ex.Message);
            }
        }

        // ================================================================ chế độ & đếm

        private void UpdateModes()
        {
            bool records = _rbRecords.Checked;

            _recordsPanel.Visible = records;
            _recordsHint.Visible = records;
            _outputGroup.Height = records ? 228 : 158;

            _btnSelectAll.Enabled = !records;
            _btnClear.Enabled = !records;
            _btnFromPartsMgmt.Enabled = !records;

            _rbPerPart.Enabled = !records;
            _folderTarget.Enabled = !records;
            _browseFolder.Enabled = !records;
            if (records && _rbPerPart.Checked)
            {
                _rbSingle.Checked = true;
            }

            UpdateCount();
            UpdateTargetPreview();
        }

        private void UpdateCount()
        {
            if (_rbRecords.Checked)
            {
                int checkedCount = 0;
                foreach (Control control in _recordsPanel.Controls)
                {
                    CheckBox box = control as CheckBox;
                    if (box != null && box.Checked) checkedCount++;
                }

                _countLabel.Text = "Chế độ theo loại bản ghi (" + checkedCount + " loại được chọn)";
                _exportButton.Enabled = checkedCount > 0;
            }
            else
            {
                _countLabel.Text = "Đã chọn: " + _checkedIds.Count + " part";
                _exportButton.Enabled = _checkedIds.Count > 0;
            }

            UpdateTargetPreview();
        }

        // ================================================================ tên file tự động + nhớ thư mục

        /// <summary>Chế độ "Một file EDZ": 1 part → tên = mã sản phẩm; nhiều part → parts.edz.</summary>
        private string SelectedSingleFileName()
        {
            List<PartEntry> selected = _entries.Where(delegate(PartEntry x) { return _checkedIds.Contains(x.Part.AdoId); }).ToList();
            if (selected.Count == 1)
            {
                string number = selected[0].Number;
                if (!string.IsNullOrWhiteSpace(number)) return SanitizeFileName(number) + ".edz";
            }
            return PartsDefaultFileName;
        }

        /// <summary>Cập nhật dòng "→ sẽ tạo: …" theo chế độ + lựa chọn hiện tại.</summary>
        private void UpdateTargetPreview()
        {
            try
            {
                if (_rbRecords.Checked)
                {
                    _targetPreview.Text = "→ sẽ tạo: " + PartsDefaultFileName + " (trong thư mục trên)";
                }
                else if (_rbSingle.Checked)
                {
                    int count = _checkedIds.Count;
                    if (count == 0)
                    {
                        _targetPreview.Text = "→ sẽ tạo: (chưa chọn part nào)";
                    }
                    else if (count == 1)
                    {
                        _targetPreview.Text = "→ sẽ tạo: " + SelectedSingleFileName() + "  (theo mã sản phẩm)";
                    }
                    else
                    {
                        _targetPreview.Text = "→ sẽ tạo: " + PartsDefaultFileName + "  (gộp " + count + " part)";
                    }
                }
                else
                {
                    _targetPreview.Text = "→ mỗi part 1 file, tên = mã sản phẩm (trong thư mục trên)";
                }
            }
            catch
            {
                // bỏ qua
            }
        }

        private static string LastFolderFile
        {
            get
            {
                return Path.Combine(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Tool Oumijia"), "export-folders.txt");
            }
        }

        private void LoadLastFolders()
        {
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string single = null;
            string perPart = null;

            try
            {
                if (File.Exists(LastFolderFile))
                {
                    string[] lines = File.ReadAllLines(LastFolderFile);
                    if (lines.Length > 0) single = lines[0];
                    if (lines.Length > 1) perPart = lines[1];
                }
            }
            catch
            {
                // không đọc được — dùng mặc định
            }

            if (string.IsNullOrWhiteSpace(_fileTarget.Text))
            {
                _fileTarget.Text = !string.IsNullOrWhiteSpace(single) && Directory.Exists(single.Trim())
                    ? single.Trim()
                    : documents;
            }
            if (string.IsNullOrWhiteSpace(_folderTarget.Text))
            {
                _folderTarget.Text = !string.IsNullOrWhiteSpace(perPart) && Directory.Exists(perPart.Trim())
                    ? perPart.Trim()
                    : documents;
            }
        }

        private void SaveLastFolders()
        {
            try
            {
                string dir = Path.GetDirectoryName(LastFolderFile);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllLines(LastFolderFile, new string[]
                {
                    (_fileTarget.Text ?? string.Empty).Trim(),
                    (_folderTarget.Text ?? string.Empty).Trim()
                });
            }
            catch
            {
                // ghi nhớ là best-effort
            }
        }

        // ================================================================ chọn đích

        private void OnBrowseFileClicked(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Chọn THƯ MỤC đích (file EDZ được đặt tên tự động)";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _fileTarget.Text = dialog.SelectedPath;
                    SaveLastFolders();
                    UpdateTargetPreview();
                }
            }
        }

        private void OnBrowseFolderClicked(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Chọn thư mục đích (mỗi part một file .edz)";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _folderTarget.Text = dialog.SelectedPath;
                    SaveLastFolders();
                    UpdateTargetPreview();
                }
            }
        }

        // ================================================================ xuất

        private void OnExportClicked(object sender, EventArgs e)
        {
            if (_rbRecords.Checked)
            {
                ExportRecordTypes();
            }
            else
            {
                ExportSelectedParts();
            }
        }

        private void ExportSelectedParts()
        {
            List<PartEntry> selected = _entries.Where(delegate(PartEntry x) { return _checkedIds.Contains(x.Part.AdoId); }).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show(this, "Chưa chọn part nào để xuất.", "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string target;
            if (_rbSingle.Checked)
            {
                string folder = (_fileTarget.Text ?? string.Empty).Trim();
                if (folder.Length == 0)
                {
                    MessageBox.Show(this, "Chưa chọn thư mục đích.", "Tool Oumijia",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try { Directory.CreateDirectory(folder); }
                catch { /* báo lỗi khi ghi */ }
                target = Path.Combine(folder, SelectedSingleFileName());

                if (File.Exists(target))
                {
                    DialogResult overwrite = MessageBox.Show(this,
                        "File đã tồn tại:\n" + target + "\n\nGhi đè?", "Tool Oumijia",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (overwrite != DialogResult.Yes) return;
                }
            }
            else
            {
                target = (_folderTarget.Text ?? string.Empty).Trim();
                if (target.Length == 0)
                {
                    MessageBox.Show(this, "Chưa chọn thư mục đích.", "Tool Oumijia",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            SaveLastFolders();

            DialogResult confirm = MessageBox.Show(this,
                "Xuất " + selected.Count + " part ra EDZ?\n\n" +
                "Đích: " + target + "\n" +
                "Cách xuất: " + (_rbSingle.Checked ? "1 file duy nhất" : "mỗi part 1 file"),
                "Tool Oumijia", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            _exportButton.Enabled = false;
            UseWaitCursor = true;
            try
            {
                if (_rbSingle.Checked)
                {
                    List<MDPart> parts = selected.Select(delegate(PartEntry x) { return x.Part; }).ToList();
                    Log("Xuất " + parts.Count + " part → " + target);
                    _database.ExportParts(target, MDPartsDatabase.DataFormat.EDZ, parts);
                    Log("Hoàn tất.");
                    AddinLog.Write("Export single OK: " + parts.Count + " parts -> " + target);
                    AddinCsvLog.Append("export", "OK-single", target, parts.Count.ToString());

                    MessageBox.Show(this, "Đã xuất " + parts.Count + " part ra:\n" + target,
                        "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    Directory.CreateDirectory(target);
                    int ok = 0, failed = 0;

                    foreach (PartEntry entry in selected)
                    {
                        string name = entry.Number.Length > 0 ? entry.Number : "part";
                        try
                        {
                            string filePath = Path.Combine(target, SanitizeFileName(name) + ".edz");
                            _database.ExportParts(filePath, MDPartsDatabase.DataFormat.EDZ, new MDPart[] { entry.Part });
                            ok++;
                            Log("OK: " + Path.GetFileName(filePath));
                            AddinCsvLog.Append("export", "OK", filePath);
                        }
                        catch (Exception exPart)
                        {
                            failed++;
                            Log("LỖI " + name + ": " + exPart.Message);
                            AddinCsvLog.Append("export", "ERROR", name, exPart.Message);
                        }
                    }

                    AddinLog.Write("Export per-part done: ok=" + ok + ", failed=" + failed + ", dir=" + target);
                    MessageBox.Show(this,
                        "Hoàn tất xuất từng part:\n• Thành công: " + ok + "\n• Lỗi: " + failed +
                        "\n\nThư mục: " + target,
                        "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Log("LỖI: " + ex.Message);
                AddinLog.WriteError("Export", ex);
                AddinCsvLog.Append("export", "ERROR", ex.Message);
                MessageBox.Show(this, "Xuất thất bại:\n" + ex.Message, "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                UpdateCount();
            }
        }

        private void ExportRecordTypes()
        {
            string folder = (_fileTarget.Text ?? string.Empty).Trim();
            if (folder.Length == 0)
            {
                MessageBox.Show(this, "Chưa chọn thư mục đích.", "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try { Directory.CreateDirectory(folder); }
            catch { /* báo lỗi khi ghi */ }
            string target = Path.Combine(folder, PartsDefaultFileName);
            SaveLastFolders();

            if (File.Exists(target))
            {
                DialogResult overwrite = MessageBox.Show(this,
                    "File đã tồn tại:\n" + target + "\n\nGhi đè?", "Tool Oumijia",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (overwrite != DialogResult.Yes) return;
            }

            List<string> checkedNames = new List<string>();
            if (_cbParts.Checked) checkedNames.Add("Parts");
            if (_cbAccessoryLists.Checked) checkedNames.Add("Accessory list");
            if (_cbAccessoryPlacements.Checked) checkedNames.Add("Accessory placement");
            if (_cbDrilling.Checked) checkedNames.Add("Drilling pattern");
            if (_cbConnectionPoints.Checked) checkedNames.Add("Connection point pattern");
            if (_cbAddresses.Checked) checkedNames.Add("Customer/Manufacturer/Supplier");

            if (checkedNames.Count == 0)
            {
                MessageBox.Show(this, "Chưa tick loại bản ghi nào để xuất.", "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult confirm = MessageBox.Show(this,
                "Xuất TOÀN BỘ bản ghi theo loại đã tick?\n\n" +
                "Loại: " + string.Join(", ", checkedNames.ToArray()) + "\n" +
                "Đích: " + target + "\n\n" +
                "Lưu ý: file kèm macro/tài liệu liên quan nên có thể lớn và mất vài phút;\n" +
                "chế độ này KHÔNG dùng tick chọn trên cây.",
                "Tool Oumijia", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            _exportButton.Enabled = false;
            UseWaitCursor = true;
            try
            {
                Log("Xuất theo loại bản ghi: " + string.Join(", ", checkedNames.ToArray()) + " → " + target);
                Stopwatch stopwatch = Stopwatch.StartNew();

                // Cờ đã kiểm chứng qua thực tế.
                _management.ExportParts(target, "IXPartsImportExportEdz", string.Empty,
                    _cbParts.Checked, null,
                    _cbAddresses.Checked, null,
                    _cbDrilling.Checked, null,
                    _cbConnectionPoints.Checked, null,
                    _cbAccessoryLists.Checked, null,
                    _cbAccessoryPlacements.Checked, null);

                stopwatch.Stop();
                long sizeMb = 0L;
                try { sizeMb = new FileInfo(target).Length / (1024 * 1024); }
                catch { }

                Log("Hoàn tất (" + sizeMb + " MB, " + stopwatch.Elapsed.TotalSeconds.ToString("0.0") + " giây).");
                AddinLog.Write("Export record-types OK: [" + string.Join("; ", checkedNames.ToArray()) +
                               "] -> " + target + " (" + sizeMb + " MB, " + stopwatch.Elapsed.TotalSeconds.ToString("0.0") + "s)");
                AddinCsvLog.Append("export", "OK-recordtypes", target, string.Join("|", checkedNames.ToArray()));

                MessageBox.Show(this,
                    "Đã xuất theo loại bản ghi:\n• " + string.Join("\n• ", checkedNames.ToArray()) +
                    "\n\nĐích: " + target + "\nKích thước: " + sizeMb + " MB",
                    "Tool Oumijia", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log("LỖI: " + ex.Message);
                AddinLog.WriteError("Export record-types", ex);
                AddinCsvLog.Append("export", "ERROR-recordtypes", ex.Message);
                MessageBox.Show(this, "Xuất thất bại:\n" + ex.Message, "Tool Oumijia",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                UpdateCount();
            }
        }

        private static string SanitizeFileName(string name)
        {
            string value = string.IsNullOrWhiteSpace(name) ? "part" : name.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }
            return value;
        }

        // ================================================================ log

        private void Log(string message)
        {
            try
            {
                _log.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + message + Environment.NewLine);
            }
            catch
            {
                // Không bao giờ để log làm hỏng thao tác.
            }
        }
    }


        //#region Oumijia.EdzScript shared UI (auto-generated from src/Oumijia.EplanAddIn/Ui; do not edit here)

    /// <summary>
    /// Cá»­a sá»• "EPLAN Classic": danh má»¥c cĂ¡c lá»‡nh quen thuá»™c trong menu cÅ©,
    /// cĂ³ thá»ƒ tĂ¬m kiáº¿m vĂ  gá»i action EPLAN Ä‘Ă£ Ä‘Æ°á»£c xĂ¡c nháº­n lĂ  cĂ³ máº·t.
    /// </summary>
    internal sealed class EplanClassicForm : Form
    {
        private const string HelpUrl = "https://github.com/wuwenhou/Tool-Oumijia-EPLAN/blob/main/docs/THIRD-PARTY-ADDINS.md";

        private readonly ListBox _categoryList = new ListBox();
        private readonly TextBox _searchBox = new TextBox();
        private readonly ListView _commands = new ListView();
        private readonly Label _statusLabel = new Label();
        private readonly Button _runButton = new Button();
        private readonly Button _helpButton = new Button();

        private readonly List<ClassicCommand> _catalog = ClassicCommand.BuildCatalog();
        private List<ClassicCommand> _filtered = new List<ClassicCommand>();

        public EplanClassicForm()
        {
            Text = "Tool Oumijia â€” EPLAN Classic";
            ClientSize = new Size(980, 560);
            MinimumSize = new Size(760, 420);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9F);

            BuildUi();
            PopulateCategories();
            ShowCategory(0);
        }

        private void BuildUi()
        {
            var leftPanel = new Panel { Dock = DockStyle.Left, Width = 185 };
            var separator = new Panel { Dock = DockStyle.Left, Width = 1, BackColor = Color.FromArgb(190, 190, 190) };
            var mainPanel = new Panel { Dock = DockStyle.Fill };

            var sourceLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                Text = "Danh má»¥c",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(6, 7, 0, 0)
            };

            _categoryList.Dock = DockStyle.Fill;
            _categoryList.IntegralHeight = false;
            _categoryList.SelectionMode = SelectionMode.One;
            _categoryList.SelectedIndexChanged += CategorySelectionChanged;
            leftPanel.Controls.Add(_categoryList);
            leftPanel.Controls.Add(sourceLabel);

            var searchPanel = new Panel { Dock = DockStyle.Top, Height = 38, Padding = new Padding(8, 7, 8, 5) };
            var searchLabel = new Label
            {
                Dock = DockStyle.Left,
                Width = 30,
                Text = "TĂ¬m:",
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };
            _searchBox.Dock = DockStyle.Fill;
            _searchBox.KeyDown += SearchBoxKeyDown;
            searchPanel.Controls.Add(_searchBox);
            searchPanel.Controls.Add(searchLabel);

            var listPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 8, 0) };
            _commands.Dock = DockStyle.Fill;
            _commands.View = View.Details;
            _commands.FullRowSelect = true;
            _commands.HideSelection = false;
            _commands.GridLines = true;
            _commands.MultiSelect = false;
            _commands.Columns.Add("Lá»‡nh", 330);
            _commands.Columns.Add("Action", 230);
            _commands.Columns.Add("Tráº¡ng thĂ¡i", 180);
            _commands.DoubleClick += OnCommandsDoubleClick;
            _commands.SelectedIndexChanged += UpdateSelection;
            listPanel.Controls.Add(_commands);

            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(8, 8, 8, 8) };
            _statusLabel.Dock = DockStyle.Left;
            _statusLabel.Width = 540;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _statusLabel.ForeColor = Color.DimGray;

            _helpButton.Dock = DockStyle.Right;
            _helpButton.Width = 92;
            _helpButton.Text = "Help";
            _helpButton.Click += OnHelpClicked;

            _runButton.Dock = DockStyle.Right;
            _runButton.Width = 110;
            _runButton.Text = "Cháº¡y lá»‡nh â–¸";
            _runButton.Enabled = false;
            _runButton.Click += OnRunClicked;

            bottomPanel.Controls.Add(_statusLabel);
            bottomPanel.Controls.Add(_runButton);
            bottomPanel.Controls.Add(_helpButton);

            mainPanel.Controls.Add(listPanel);
            mainPanel.Controls.Add(searchPanel);
            mainPanel.Controls.Add(bottomPanel);

            Controls.Add(mainPanel);
            Controls.Add(separator);
            Controls.Add(leftPanel);
        }

        private void PopulateCategories()
        {
            foreach (var category in _catalog.Select(x => x.Category).Distinct())
            {
                _categoryList.Items.Add(category);
            }

            // LuĂ´n hiá»ƒn thá»‹ Ä‘Ăºng 12 nhĂ³m cá»§a menu cá»• Ä‘iá»ƒn, ká»ƒ cáº£ khi nhĂ³m chÆ°a cĂ³ action usable.
            foreach (string category in ClassicCommand.MenuCategories)
            {
                if (!_categoryList.Items.Contains(category))
                {
                    _categoryList.Items.Add(category);
                }
            }

            if (_categoryList.Items.Count > 0)
            {
                _categoryList.SelectedIndex = 0;
            }
        }

        private void CategorySelectionChanged(object sender, EventArgs e)
        {
            ShowCategory(_categoryList.SelectedIndex);
        }

        private void ShowCategory(int index)
        {
            if (index < 0) return;
            string category = _categoryList.Items[index].ToString();
            ApplyFilter(_searchBox.Text, category);
        }

        private void SearchBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            string category = CurrentCategory();
            ApplyFilter(_searchBox.Text, category);
        }

        private void ApplyFilter(string query, string category)
        {
            query = (query ?? string.Empty).Trim().ToLowerInvariant();
            _filtered = _catalog
                .Where(x => x.Category == category)
                .Where(x => query.Length == 0
                    || x.Name.ToLowerInvariant().Contains(query)
                    || x.ActionId.ToLowerInvariant().Contains(query)
                    || (x.Source ?? string.Empty).ToLowerInvariant().Contains(query))
                .OrderBy(x => x.MenuIndex)
                .ToList();

            _commands.BeginUpdate();
            try
            {
                _commands.Items.Clear();
                foreach (ClassicCommand command in _filtered)
                {
                    bool available = command.IsAvailable();
                    ListViewItem item = new ListViewItem(command.Name);
                    item.SubItems.Add(command.ActionId);
                    item.SubItems.Add(available ? "Sáºµn sĂ ng" : "KhĂ´ng cĂ³ trong phiĂªn nĂ y");
                    item.Tag = command;
                    item.BackColor = available ? Color.White : Color.FromArgb(245, 245, 245);
                    item.ForeColor = available ? Color.Black : Color.DimGray;
                    _commands.Items.Add(item);
                }
            }
            finally
            {
                _commands.EndUpdate();
            }

            if (_filtered.Count == 0)
            {
                _statusLabel.Text = query.Length == 0
                    ? "ChÆ°a xĂ¡c Ä‘á»‹nh Ä‘Æ°á»£c lá»‡nh cá»§a nhĂ³m nĂ y trĂªn phiĂªn EPLAN hiá»‡n táº¡i."
                    : "KhĂ´ng tĂ¬m tháº¥y lá»‡nh phĂ¹ há»£p trong nhĂ³m nĂ y.";
            }
            else
            {
                _statusLabel.Text = _filtered.Count + " lá»‡nh; hĂ nh Ä‘á»™ng khĂ´ng sáºµn sĂ ng sáº½ bá»‹ khĂ³a.";
            }

            UpdateSelection(this, EventArgs.Empty);
        }

        private void UpdateSelection(object sender, EventArgs e)
        {
            ClassicCommand selection = SelectedCommand();
            _runButton.Enabled = selection != null && selection.IsAvailable();
            _statusLabel.Text = selection == null
                ? "Chá»n má»™t lá»‡nh Ä‘á»ƒ xem tráº¡ng thĂ¡i."
                : (selection.IsAvailable() ? "Sáºµn sĂ ng cháº¡y: " + selection.ActionId : "KhĂ´ng sáºµn sĂ ng: " + selection.Note);
        }

        private ClassicCommand SelectedCommand()
        {
            if (_commands.SelectedItems.Count == 0) return null;
            return _commands.SelectedItems[0].Tag as ClassicCommand;
        }

        private string CurrentCategory()
        {
            return _categoryList.SelectedIndex < 0 ? ClassicCommand.MenuCategories[0] : _categoryList.Items[_categoryList.SelectedIndex].ToString();
        }

        private void OnCommandsDoubleClick(object sender, EventArgs e)
        {
            RunSelected();
        }

        private void OnRunClicked(object sender, EventArgs e)
        {
            RunSelected();
        }

        private void RunSelected()
        {
            ClassicCommand command = SelectedCommand();
            if (command == null) return;

            if (!command.IsAvailable())
            {
                MessageBox.Show(this,
                    "Lá»‡nh chÆ°a sáºµn sĂ ng:\n" + command.Note,
                    "Tool Oumijia â€” EPLAN Classic",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                bool ok = command.Run();
                _statusLabel.Text = ok ? "ÄĂ£ cháº¡y: " + command.ActionId : "Action tráº£ vá» lá»—i: " + command.ActionId;
                AddinLog.Write("EPLAN Classic: ran " + command.ActionId + " (ok=" + ok + ")");
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("EPLAN Classic run " + command.ActionId, ex);
                MessageBox.Show(this,
                    "KhĂ´ng cháº¡y Ä‘Æ°á»£c lá»‡nh:\n" + ex.Message,
                    "Tool Oumijia â€” EPLAN Classic",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnHelpClicked(object sender, EventArgs e)
        {
            try
            {
                Process.Start(HelpUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "KhĂ´ng má»Ÿ Ä‘Æ°á»£c help:\n" + ex.Message,
                    "Tool Oumijia â€” EPLAN Classic",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    /// <summary>
    /// Action dĂ¹ng cho ribbon/script khi EPLAN Classic cáº§n má»Ÿ cá»­a sá»• tá»« ngoĂ i.
    /// </summary>
    internal static class EplanClassicLauncher
    {
        public static bool Open()
        {
            try
            {
                using (var form = new EplanClassicForm())
                {
                    form.ShowDialog();
                }
                return true;
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("EPLAN Classic open", ex);
                return false;
            }
        }
    }

    internal sealed class ClassicCommand
    {
        public static readonly string[] MenuCategories =
        {
            "Project", "Page", "Layout space", "Edit", "View", "Insert",
            "Project data", "Find", "Options", "Utilities", "Window", "Help"
        };

        public readonly string Category;
        public readonly string Name;
        public readonly string ActionId;
        public readonly string Note;
        public readonly string Source;
        public readonly int MenuIndex;

        private ClassicCommand(string category, string name, string actionId, string source, string note, int menuIndex)
        {
            Category = category;
            Name = name;
            ActionId = actionId;
            Source = source;
            Note = note;
            MenuIndex = menuIndex;
        }

        public static List<ClassicCommand> BuildCatalog()
        {
            var result = new List<ClassicCommand>();
            Add(result, "Project", "Má»Ÿ project", "ProjectOpen", "regPlatform.xml", "Action má»Ÿ há»™p chá»n project", 0);
            Add(result, "Project", "So sĂ¡nh project", "PrjManagementGUIIGfWindPrjCompareProject", "regPlatform.xml", "Má»Ÿ so sĂ¡nh project", 1);
            Add(result, "Project", "Backup project", "BackupProjectDialogShow", "regPlatform.xml", "Má»Ÿ há»™p Backup project", 2);
            Add(result, "Project", "NĂ©n project", "CompressProjectDialogShow", "regPlatform.xml", "Má»Ÿ há»™p nĂ©n project", 3);
            Add(result, "Project", "ÄĂ³ng project hiá»‡n táº¡i", "PrjManagementGUIIGfWindPrjCloseProject", "regPlatform.xml", "ÄĂ³ng project hiá»‡n táº¡i", 4);

            Add(result, "Page", "ThĂªm trang", "XPageNewDialogShow", "regPlatform.xml", "Má»Ÿ há»™p thĂªm trang", 0);
            Add(result, "Page", "Trang káº¿ tiáº¿p", "XGedNextPageAction", "regPlatform.xml", "Chuyá»ƒn sang trang káº¿ tiáº¿p", 1);
            Add(result, "Page", "Trang trÆ°á»›c", "XGedPrevPageAction", "regPlatform.xml", "Chuyá»ƒn sang trang trÆ°á»›c", 2);
            Add(result, "Page", "ÄĂ³ng trang", "XGedClosePage", "regPlatform.xml", "ÄĂ³ng trang hiá»‡n táº¡i", 3);
            Add(result, "Page", "Thuá»™c tĂ­nh trang", "XPagePropertyDialogShow", "regPlatform.xml", "Má»Ÿ thuá»™c tĂ­nh trang", 4);

            Add(result, "Edit", "CÄƒn lá» ngang", "XGedIaAlignHorizontally", "regPlatform.xml", "CÄƒn lá» theo chiá»u ngang", 0);
            Add(result, "Edit", "CÄƒn lá» dá»c", "XGedIaAlignVertically", "regPlatform.xml", "CÄƒn lá» theo chiá»u dá»c", 1);
            Add(result, "Edit", "CÄƒn Ä‘áº·t lĂªn lÆ°á»›i", "XGedIaArrangeOnGrid", "regPlatform.xml", "Cáº§n selection phĂ¹ há»£p; EPLAN kiá»ƒm tra Ä‘iá»u kiá»‡n", 2);
            Add(result, "Edit", "TĂ¬m pháº§n tá»­ khĂ´ng cĂ³ property", "XKBeFindDmObjectsWithoutProperties", "regPlatform.xml", "Danh sĂ¡ch pháº§n tá»­ thiáº¿u property", 3);

            Add(result, "View", "Báº­t/Táº¯t Menu Bar", "MfToggleMainMenuAction", "regPlatform.xml", "Toggle thanh menu chĂ­nh cá»§a EPLAN (MF)", 0);
            Add(result, "View", "Báº­t/Táº¯t báº¯t dĂ­nh lÆ°á»›i", "XGedToggleGridSnapAction", "regPlatform.xml", "Chá»‰ Ă¡p dá»¥ng khi cá»­a sá»• váº½ EPLAN Ä‘ang má»Ÿ", 1);
            Add(result, "View", "Báº­t/Táº¯t báº¯t dĂ­nh Ä‘á»‘i tÆ°á»£ng", "XGedToggleObjectSnapAction", "regPlatform.xml", "Chá»‰ Ă¡p dá»¥ng khi cá»­a sá»• váº½ EPLAN Ä‘ang má»Ÿ", 2);
            Add(result, "View", "Báº­t/Táº¯t Design mode", "XGedToggleCursorDesignAction", "regPlatform.xml", "Cursor placement cho special interactions", 3);
            Add(result, "View", "Báº­t/Táº¯t hiá»ƒn thá»‹ lÆ°á»›i", "XGedToggleGridVisibilityAction", "regPlatform.xml", "Chá»‰ Ă¡p dá»¥ng khi cĂ³ cá»­a sá»• EPLAN", 4);

            Add(result, "Project data", "Nháº­p Parts List", "ImportPartslistAction", "regPlatform.xml", "Nháº­p parts list theo converter EPLAN", 0);
            Add(result, "Project data", "Xuáº¥t Parts List", "ExportPartslistAction", "regPlatform.xml", "Xuáº¥t parts list theo converter EPLAN", 1);
            Add(result, "Project data", "Nháº­p Parts quáº£n lĂ½ báº±ng GUI", "PrjManagementGUIIGfWindImportProject", "regPlatform.xml", "Nháº­p project theo thao tĂ¡c EPLAN", 2);
            Add(result, "Project data", "Xuáº¥t project", "PrjManagementGUIIGfWindPrjExportProject", "regPlatform.xml", "Má»Ÿ thao tĂ¡c xuáº¥t project", 3);

            Add(result, "Find", "TĂ¬m trong EPLAN", "XSGedFindAction", "regPlatform.xml", "TĂ¬m Ä‘á»‘i tÆ°á»£ng/dá»¯ liá»‡u trong dá»± Ă¡n", 0);
            Add(result, "Find", "TĂ¬m connection point ngoĂ i lÆ°á»›i", "XSGedFindConnectionPointsOutOfRasterAction", "regPlatform.xml", "TĂ¬m connection point náº±m ngoĂ i raster", 1);

            Add(result, "Options", "Import settings", "AfImportSettingsAction", "regPlatform.xml", "Nháº­p settings tá»« file EPLAN", 0);
            Add(result, "Options", "Export settings", "AfExportSettingsAction", "regPlatform.xml", "Xuáº¥t settings ra file EPLAN", 1);
            Add(result, "Options", "Thiáº¿t láº­p hiá»ƒn thá»‹ lÆ°á»›i", "XGedToggleGridVisibilityAction", "regPlatform.xml", "Toggle hiá»ƒn thá»‹ lÆ°á»›i", 2);

            Add(result, "Utilities", "Dá»‹ch database", "translate", "regPlatform.xml", "Cháº¡y thao tĂ¡c dá»‹ch cá»§a EPLAN (kiá»ƒm tra trÆ°á»›c khi cháº¡y hĂ ng loáº¡t)", 0);
            Add(result, "Utilities", "Backup project", "BackupProjectDialogShow", "regPlatform.xml", "Má»Ÿ há»™p Backup project", 1);

            return result;
        }

        private static void Add(List<ClassicCommand> list, string category, string name, string actionId, string source, string note, int menuIndex)
        {
            list.Add(new ClassicCommand(category, name, actionId, source, note, menuIndex));
        }

        public bool IsAvailable()
        {
            if (string.IsNullOrWhiteSpace(ActionId)) return false;
            try
            {
                var manager = new ActionManager();
                var action = manager.FindAction(ActionId);
                return action != null;
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("ClassicCommand IsAvailable " + ActionId, ex);
                return false;
            }
        }

        public bool Run()
        {
            if (!IsAvailable()) return false;

            var manager = new ActionManager();
            var action = manager.FindAction(ActionId);
            if (action == null) return false;

            var context = new ActionCallingContext();
            return action.Execute(context);
        }
    }


    internal static class VeeIntegration
    {
        private const string ExportConnectionsAction = "VE_E_ExportConnections";
        private const string TableMakerAction = "VEECreateEplanTable";

        public static bool IsAvailable(string actionName)
        {
            try
            {
                var manager = new ActionManager();
                return manager.FindAction(actionName) != null;
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("VEE availability " + actionName, ex);
                return false;
            }
        }

        public static bool OpenExportConnections()
        {
            return OpenOrReport(null, ExportConnectionsAction, "VEE Export connections data",
                "https://t.me/EPLAN4UCHAT/13646",
                "TĂ­nh nÄƒng nĂ y hÆ°á»›ng dáº«n EPLAN Platform 2022+ / .NET Framework 4.8+.\n" +
                "Báº£n cĂ i Ä‘áº·t & báº£n quyá»n thuá»™c VIET EPLAN & ELECTRIC; Tool Oumijia khĂ´ng phĂ¢n phá»‘i láº¡i DLL.\n\n" +
                "Sau khi cĂ i, restart EPLAN vĂ  báº¥m nĂºt nĂ y láº§n ná»¯a.");
        }

        public static void OpenExportConnections(IWin32Window owner)
        {
            OpenExportConnections();
        }

        public static bool OpenTableMaker()
        {
            return OpenOrReport(null, TableMakerAction, "VEE Table Maker",
                "https://t.me/EPLAN4U/12788",
                "Táº¡o technical tables trá»±c tiáº¿p trĂªn trang EPLAN.\n" +
                "Báº£n cĂ i Ä‘áº·t & báº£n quyá»n thuá»™c VIET EPLAN & ELECTRIC; Tool Oumijia khĂ´ng phĂ¢n phá»‘i láº¡i DLL.\n\n" +
                "Sau khi cĂ i, restart EPLAN vĂ  báº¥m nĂºt nĂ y láº§n ná»¯a.");
        }

        public static void OpenTableMaker(IWin32Window owner)
        {
            OpenTableMaker();
        }

        private static bool OpenOrReport(IWin32Window owner, string actionName, string displayName, string downloadUrl, string helpText)
        {
            try
            {
                var manager = new ActionManager();
                var action = manager.FindAction(actionName);
                if (action != null)
                {
                    bool ok = action.Execute(new ActionCallingContext());
                    AddinLog.Write("VEE: invoked " + actionName + " (ok=" + ok + ")");
                    return ok;
                }
            }
            catch (Exception ex)
            {
                AddinLog.WriteError("VEE invoke " + actionName, ex);
                MessageBox.Show(owner,
                    "KhĂ´ng cháº¡y Ä‘Æ°á»£c " + displayName + ":\n" + ex.Message,
                    "Tool Oumijia â€” VEE",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            var choice = MessageBox.Show(owner,
                displayName + " chÆ°a Ä‘Æ°á»£c Ä‘Äƒng kĂ½/cĂ i Ä‘áº·t trĂªn mĂ¡y nĂ y.\n\n" + helpText,
                "Tool Oumijia â€” VEE",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (choice == DialogResult.Yes)
            {
                try { Process.Start(downloadUrl); }
                catch (Exception ex)
                {
                    AddinLog.WriteError("Open VEE link", ex);
                    MessageBox.Show(owner,
                        "KhĂ´ng má»Ÿ Ä‘Æ°á»£c link. Sao chĂ©p:\n" + downloadUrl,
                        "Tool Oumijia â€” VEE",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            return false;
        }
    }
        //#endregion
}
