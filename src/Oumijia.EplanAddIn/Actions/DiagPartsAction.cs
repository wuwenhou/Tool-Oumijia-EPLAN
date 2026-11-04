using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.Base;
using Eplan.EplApi.MasterData;
using Oumijia.EplanAddIn.Ui;

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
    }
}
