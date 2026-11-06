using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.Base;
using Eplan.EplApi.MasterData;
using Oumijia.EplanUi;

namespace Oumijia.EplanAddIn.Actions
{
    /// <summary>
    /// Action chẩn đoán ẩn "OumijiaDiagParts": dùng headless để kiểm tra pipeline dữ liệu part:
    /// 1) mở parts database (SelectedPartsDatabase → thử OpenDatabase)
    /// 2) đếm part + đo thời gian đọc property + tóm tắt cây nhóm
    /// 3) smoke test: export 25 part đầu ra EDZ trong %TEMP%\OumijiaDiag
    ///
    /// Chạy:
    ///   W3u.exe /Auto /Quiet /NoSplash /NoLicenseDialog /NoUserRightsDialog /NoLoadWorkspace OumijiaDiagParts
    /// Kết quả: %APPDATA%\Tool Oumijia\logs\addin.log
    /// </summary>
    public class DiagPartsAction : IEplAction
    {
        public bool Execute(ActionCallingContext oActionCallingContext)
        {
            var report = new StringBuilder();
            report.AppendLine("=== DIAG PARTS " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ===");

            // Kiểm tra construct UI (không show) — bắt lỗi khởi tạo/layout sớm.
            try
            {
                using (var form = new ImportEdzForm()) { }
                report.AppendLine("UI ImportEdzForm construct: OK");
            }
            catch (Exception exUi1)
            {
                report.AppendLine("UI ImportEdzForm construct LỖI: " + exUi1);
            }
            try
            {
                using (var form = new ExportEdzForm()) { }
                report.AppendLine("UI ExportEdzForm construct: OK");
            }
            catch (Exception exUi2)
            {
                report.AppendLine("UI ExportEdzForm construct LỖI: " + exUi2);
            }

            MDPartsManagement management = null;
            MDPartsDatabase database = null;
            try
            {
                management = new MDPartsManagement();

                try
                {
                    database = management.SelectedPartsDatabase;
                    if (database != null)
                    {
                        report.AppendLine("Database: SelectedPartsDatabase");
                    }
                }
                catch (Exception ex)
                {
                    report.AppendLine("SelectedPartsDatabase lỗi: " + ex.Message);
                }

                if (database == null)
                {
                    foreach (var name in new[] { "ESS_part001" })
                    {
                        try
                        {
                            database = management.OpenDatabase(name);
                            report.AppendLine("Database: OpenDatabase('" + name + "')");
                            break;
                        }
                        catch (Exception ex)
                        {
                            report.AppendLine("OpenDatabase('" + name + "') lỗi: " + ex.Message);
                        }
                    }
                }

                if (database == null)
                {
                    try
                    {
                        database = management.OpenDatabase();
                        report.AppendLine("Database: OpenDatabase() (system)");
                    }
                    catch (Exception ex)
                    {
                        report.AppendLine("OpenDatabase() lỗi: " + ex.Message);
                    }
                }

                if (database == null)
                {
                    report.AppendLine("KHÔNG mở được database nào — bỏ qua kiểm tra.");
                    return true;
                }

                var stopwatch = Stopwatch.StartNew();
                var parts = new List<MDPart>(database.Parts);
                var enumMs = stopwatch.ElapsedMilliseconds;
                report.AppendLine("Parts: " + parts.Count + " (enum " + enumMs + " ms)");

                stopwatch.Restart();
                var entries = new List<PartEntry>(parts.Count);
                foreach (var part in parts)
                {
                    entries.Add(PartEntry.FromPart(part));
                }
                stopwatch.Stop();
                report.AppendLine("Đọc property " + parts.Count + " part: " + stopwatch.ElapsedMilliseconds + " ms");

                var model = PartsTreeModel.Build(entries);
                report.AppendLine("Cây nhóm — " + model.Root.Children.Count + " nhóm cấp cao nhất:");
                foreach (var line in model.DescribeTopGroups(25))
                {
                    report.AppendLine(line);
                }

                if (entries.Count > 0)
                {
                    report.AppendLine("Mẫu part[0]: " + entries[0].Label + " | path=" + string.Join(" / ", entries[0].Path));
                }

                // Smoke export: 25 part đầu ra file EDZ tạm.
                var targetDir = Path.Combine(Path.GetTempPath(), "OumijiaDiag");
                Directory.CreateDirectory(targetDir);
                var file = Path.Combine(targetDir, "smoke-" + DateTime.Now.ToString("HHmmss") + ".edz");

                var sample = new List<MDPart>();
                for (var i = 0; i < entries.Count && sample.Count < 25; i++)
                {
                    sample.Add(entries[i].Part);
                }

                if (sample.Count > 0)
                {
                    try
                    {
                        database.ExportParts(file, MDPartsDatabase.DataFormat.EDZ, sample);
                        var info = new FileInfo(file);
                        report.AppendLine("SMOKE EXPORT OK: " + sample.Count + " part -> " + file +
                                          " (" + (info.Exists ? info.Length : 0) + " bytes)");
                    }
                    catch (Exception ex)
                    {
                        report.AppendLine("SMOKE EXPORT LỖI: " + ex.Message);
                    }
                }

                // ----- record-type export combos: mỗi lần bật đúng 1 cờ để xác minh
                // cờ API <-> loại bản ghi (giải nén EDZ đối chiếu).
                report.AppendLine("--- Record-type export combos ---");
                TryExportCombo(report, management, "rt-01-parts", true, false, false, false, false, false);
                TryExportCombo(report, management, "rt-02-addresses", false, true, false, false, false, false);
                TryExportCombo(report, management, "rt-03-constructions", false, false, true, false, false, false);
                TryExportCombo(report, management, "rt-04-terminals", false, false, false, true, false, false);
                TryExportCombo(report, management, "rt-05-accessorylists", false, false, false, false, true, false);
                TryExportCombo(report, management, "rt-06-accessoryplacements", false, false, false, false, false, true);
                TryExportCombo(report, management, "rt-07-all", true, true, true, true, true, true);

                // ----- import smoke test: export 1 part rồi NHẬP LẠI (kiểm chứng scheme nhập)
                try
                {
                    var importFile = Path.Combine(targetDir, "import-smoke.edz");
                    var one = new List<MDPart>();
                    if (entries.Count > 0) one.Add(entries[0].Part);

                    if (one.Count > 0)
                    {
                        database.ExportParts(importFile, MDPartsDatabase.DataFormat.EDZ, one);

                        var candidates = ImportPartsSupport.ResolveSchemeCandidates();
                        report.AppendLine("IMPORT scheme candidates: " + string.Join(" | ", candidates.ToArray()));

                        var files = new System.Collections.ArrayList();
                        files.Add(importFile);
                        var used = ImportPartsSupport.Import(management, files,
                            MDPartsManagement.ImportMode.UpdateAndAppend, false,
                            MDPartsManagement.ImportIdentifier.PartNumber);
                        report.AppendLine("IMPORT SMOKE OK: 1 part re-imported (scheme='" + used + "')");
                    }
                }
                catch (Exception exImport)
                {
                    report.AppendLine("IMPORT SMOKE LỖI: " + exImport.Message);
                }
            }
            catch (Exception ex)
            {
                report.AppendLine("FATAL: " + ex);
            }

            AddinLog.Write(report.ToString());
            return true;
        }

        public bool OnRegister(ref string Name, ref int Ordinal)
        {
            Name = "OumijiaDiagParts";
            Ordinal = 24;
            return true;
        }

        public void GetActionProperties(ref ActionProperties actionProperties)
        {
            // EPLAN 2022: ActionProperties không có member public ghi được.
        }

        // ------------------------------------------------------------------ helpers

        private static void TryExportCombo(StringBuilder report, MDPartsManagement management, string name,
            bool parts, bool addresses, bool constructions, bool terminals, bool accessoryLists, bool accessoryPlacements)
        {
            var dir = Path.Combine(Path.GetTempPath(), "OumijiaDiag");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, name + ".edz");

            try { if (File.Exists(file)) File.Delete(file); }
            catch { /* ignore */ }

            try
            {
                try
                {
                    management.ExportParts(file, "IXPartsImportExportEdz", string.Empty,
                        parts, null, addresses, null, constructions, null, terminals, null,
                        accessoryLists, null, accessoryPlacements, null);
                }
                catch (Exception)
                {
                    // Một số bản chỉ chấp nhận filter object thay vì null.
                    management.ExportParts(file, "IXPartsImportExportEdz", string.Empty,
                        parts, new MDObjectFilter(), addresses, new MDObjectFilter(),
                        constructions, new MDObjectFilter(), terminals, new MDObjectFilter(),
                        accessoryLists, new MDObjectFilter(), accessoryPlacements, new MDObjectFilter());
                }

                var info = new FileInfo(file);
                report.AppendLine("RT " + name + ": OK (" + (info.Exists ? info.Length : 0) + " bytes)");
            }
            catch (Exception ex)
            {
                report.AppendLine("RT " + name + ": LỖI: " + ex.Message);
            }
        }
    }
}
