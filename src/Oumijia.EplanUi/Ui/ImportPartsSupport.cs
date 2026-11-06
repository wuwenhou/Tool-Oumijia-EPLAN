using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Eplan.EplApi.Base;
using Eplan.EplApi.MasterData;

namespace Oumijia.EplanUi
{
    /// <summary>
    /// Gọi <see cref="MDPartsManagement.ImportParts"/> với field-mapping scheme HỢP LỆ.
    ///
    /// EPLAN từ chối scheme rỗng: "S030060:Invalid scheme. Parameter value: ''..."
    /// (EPLAN's dialog tự truyền scheme mặc định nên không lỗi).
    ///
    /// Thứ tự dò scheme:
    ///   1. USER.PartsManagementGui.FieldMapping.LastUsed  (scheme EPLAN đang dùng)
    ///   2. STATION.PartsManagementGui.FieldMapping.LastUsed
    ///   3. Hằng số "Standardvorgabe EPLAN" (scheme built-in)
    ///   4. Nếu vẫn lỗi: đọc tên scheme từ chính thông báo lỗi rồi thử lại
    ///      (thông báo: "Please use one of the following schemes: 'X'").
    /// </summary>
    public static class ImportPartsSupport
    {
        public const string Converter = "IXPartsImportExportEdz";
        public const string FallbackScheme = "Standardvorgabe EPLAN";

        /// <summary>Nhập danh sách file; trả về tên scheme đã dùng thành công.</summary>
        public static string Import(
            MDPartsManagement management,
            ArrayList files,
            MDPartsManagement.ImportMode mode,
            bool additionalLanguage,
            MDPartsManagement.ImportIdentifier identifier)
        {
            if (management == null) throw new ArgumentNullException("management");

            var candidates = ResolveSchemeCandidates();
            Exception lastSchemeError = null;

            // Duyệt trên bản sao — danh sách ứng viên có thể được bổ sung từ thông báo lỗi.
            foreach (var scheme in candidates.ToArray())
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
                    foreach (var parsed in ParseSchemeNames(ex.Message))
                    {
                        if (!candidates.Contains(parsed)) candidates.Add(parsed);
                    }
                }
            }

            throw lastSchemeError ?? new InvalidOperationException(
                "Không tìm được field-mapping scheme để nhập EDZ.");
        }

        /// <summary>Danh sách scheme ứng viên theo thứ tự ưu tiên (không bao giờ rỗng).</summary>
        public static List<string> ResolveSchemeCandidates()
        {
            var result = new List<string>();

            foreach (var path in new[]
            {
                "USER.PartsManagementGui.FieldMapping.LastUsed",
                "STATION.PartsManagementGui.FieldMapping.LastUsed"
            })
            {
                try
                {
                    var settings = new Settings();
                    if (settings.ExistSetting(path))
                    {
                        var value = settings.GetStringSetting(path, 0);
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
            var message = exception == null ? string.Empty : (exception.Message ?? string.Empty);

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
            var names = new List<string>();
            if (string.IsNullOrEmpty(message)) return names;

            var matches = Regex.Matches(message, "'([^']+)'");
            foreach (Match match in matches)
            {
                var name = match.Groups[1].Value.Trim();
                if (name.Length > 0 && !names.Contains(name)) names.Add(name);
            }
            return names;
        }
    }
}
