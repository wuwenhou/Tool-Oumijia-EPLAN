using System;
using System.Collections.Generic;
using Eplan.EplApi.Base;
using Eplan.EplApi.MasterData;

namespace Oumijia.EplanAddIn.Ui
{
    /// <summary>
    /// Bọc một MDPart với thông tin hiển thị/định vị cây đã đọc sẵn (đọc property đúng 1 lần —
    /// tránh gọi API lặp lại khi lọc/tìm kiếm).
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
            var number = PartInfo.ReadText(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_PARTNR);
            if (number.Length == 0)
            {
                try { number = (part.PartNr ?? string.Empty).Trim(); } catch { /* ignore */ }
            }

            var designation = PartInfo.ReadText(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_DESCR1);

            var manufacturer = PartInfo.ReadText(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_MANUFACTURER);
            if (manufacturer.Length == 0)
            {
                manufacturer = PartInfo.ReadText(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_MANUFACTURER_NAME);
            }

            var label = number;
            if (designation.Length > 0 && !string.Equals(designation, number, StringComparison.OrdinalIgnoreCase))
            {
                label = number + " (" + designation + ")";
            }

            var path = new List<string>(4);

            var top = PartInfo.ReadText(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_PRODUCTTOPGROUP);
            path.Add(top.Length > 0 ? top : "(Không nhóm)");

            var group = PartInfo.ReadText(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_PRODUCTGROUP);
            if (group.Length > 0) path.Add(group);

            var sub = PartInfo.ReadText(part, Eplan.EplApi.MasterData.Properties.MDPartsDatabaseItem.ARTICLE_PRODUCTSUBGROUP);
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
    /// Đọc chuỗi hiển thị của property part — xử lý cả 3 kiểu giá trị của EPLAN:
    /// 1) thuộc tính enum (vd nhóm sản phẩm: giá trị số "26" → hiển thị "PLC")
    /// 2) đa ngôn ngữ ("de_DE@…;en_US@LOGO! 24RC;" → "LOGO! 24RC")
    /// 3) chuỗi thường ("SIE").
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
                var multiLang = value.GetDisplayString();
                if (multiLang != null)
                {
                    var display = Clean(multiLang.GetStringToDisplay(ISOCode.Language.L_en_US));
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
                var localized = Clean(value.ToString(ISOCode.Language.L_en_US));
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

            foreach (var part in raw.Split(';'))
            {
                var segment = part.Trim();
                if (segment.Length == 0) continue;

                string language;
                string text;
                var at = segment.IndexOf('@');
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
            foreach (var c in value)
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
            var model = new PartsTreeModel();
            foreach (var entry in entries)
            {
                var node = model.Root;
                foreach (var segment in entry.Path)
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
            node.Entries.Sort((a, b) => string.Compare(a.Number, b.Number, StringComparison.OrdinalIgnoreCase));
            var total = node.Entries.Count;
            foreach (var child in node.Children.Values)
            {
                total += ComputeTotalsAndSort(child);
            }
            node.TotalParts = total;
            return total;
        }

        /// <summary>Dòng mô tả cho log chẩn đoán (số part theo nhóm cấp cao nhất).</summary>
        public IEnumerable<string> DescribeTopGroups(int max)
        {
            var count = 0;
            foreach (var child in Root.Children.Values)
            {
                if (count++ >= max) yield break;
                yield return "  " + child.Name + ": " + child.TotalParts + " part";
            }
        }
    }
}
