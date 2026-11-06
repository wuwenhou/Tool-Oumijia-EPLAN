using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Oumijia.EdzReader
{
    /// <summary>Tolerant local reader for factual part metadata inside EDZ packages.</summary>
    public static class PartXmlParser
    {
        private const int MaxProperties = 4000;
        private const int MaxStoredValueLength = 4000;
        private const int MaxFieldLength = 300;

        private static readonly KeyValuePair<string, string>[] RegexPatterns = new KeyValuePair<string, string>[]
        {
            new KeyValuePair<string, string>("P_ARTICLE_MANUFACTURER", @"P[_\-. ]?ARTICLE[_\-. ]?MANUFACTURER[^>]{0,150}>\s*([^<]{1,200})<"),
            new KeyValuePair<string, string>("P_ARTICLE_PARTNR", @"P[_\-. ]?ARTICLE[_\-. ]?PARTNR[^>]{0,150}>\s*([^<]{1,200})<"),
            new KeyValuePair<string, string>("P_ARTICLE_TYPENR", @"P[_\-. ]?ARTICLE[_\-. ]?TYPENR[^>]{0,150}>\s*([^<]{1,200})<"),
            new KeyValuePair<string, string>("P_ARTICLE_ORDERNR", @"P[_\-. ]?ARTICLE[_\-. ]?ORDERNR[^>]{0,150}>\s*([^<]{1,200})<"),
            new KeyValuePair<string, string>("MANUFACTURER", @"<[^>]{0,60}MANUFACTURER[^>]{0,60}>\s*([^<]{1,200})<"),
            new KeyValuePair<string, string>("PARTNUMBER", @"<[^>]{0,60}PART[_\s]?NUMBER[^>]{0,60}>\s*([^<]{1,200})<")
        };

        public static EdzPartInfo Parse(string xmlText, string sourceEntry)
        {
            if (String.IsNullOrWhiteSpace(xmlText)) return null;

            Dictionary<string, string> props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try { HarvestXml(xmlText, props); } catch { /* Fall back to regex. */ }
            if (props.Count == 0) HarvestRegex(xmlText, props);

            EdzPartInfo part = new EdzPartInfo();
            part.SourceXmlEntry = sourceEntry ?? String.Empty;
            foreach (KeyValuePair<string, string> kv in props) part.Properties[kv.Key] = kv.Value;

            part.Manufacturer = Find(props, new[] { "MANUFACTURER", "HERSTELLER", "MAKER" });
            part.PartNumber = Find(props, new[] { "PARTNR", "PARTNUMBER", "PART_NO", "ARTIKELNR" });
            part.TypeNumber = Find(props, new[] { "TYPENR", "TYPENUMBER", "TYPEDESIGNATION" });
            part.OrderNumber = Find(props, new[] { "ORDERNR", "ORDERNUMBER", "BESTELLNR" });
            part.Designation = Find(props, new[] { "DESIGNATION", "DESCR1", "DESCRIPTION", "BENENNUNG" });
            part.ProductGroup = Find(props, new[] { "PRODUCTTOPGROUP", "PRODUCTGROUP", "PRODUCTSUBGROUP", "WARENGRUPPE" });
            part.EClassCode = Find(props, new[] { "ECLASS" });

            if (String.IsNullOrEmpty(part.PartNumber) &&
                String.IsNullOrEmpty(part.TypeNumber) &&
                String.IsNullOrEmpty(part.Manufacturer)) return null;
            return part;
        }

        private static void HarvestXml(string xml, Dictionary<string, string> props)
        {
            XDocument doc = XDocument.Parse(xml, LoadOptions.None);
            if (doc.Root == null) return;

            foreach (XElement el in doc.Root.DescendantsAndSelf())
            {
                if (props.Count >= MaxProperties) return;
                string text = DirectText(el);
                AddProperty(props, el.Name.LocalName, text);

                foreach (XAttribute attr in el.Attributes())
                {
                    string attrName = attr.Name.LocalName;
                    if (String.Equals(attrName, "id", StringComparison.OrdinalIgnoreCase) ||
                        String.Equals(attrName, "name", StringComparison.OrdinalIgnoreCase) ||
                        String.Equals(attrName, "key", StringComparison.OrdinalIgnoreCase))
                    {
                        AddProperty(props, attr.Value, text);
                    }
                }

                if (el.HasElements)
                {
                    XElement nameEl = null;
                    XElement valueEl = null;
                    foreach (XElement child in el.Elements())
                    {
                        string n = child.Name.LocalName;
                        if (nameEl == null && (String.Equals(n, "name", StringComparison.OrdinalIgnoreCase) ||
                                               String.Equals(n, "key", StringComparison.OrdinalIgnoreCase) ||
                                               String.Equals(n, "id", StringComparison.OrdinalIgnoreCase))) nameEl = child;
                        else if (valueEl == null && (String.Equals(n, "value", StringComparison.OrdinalIgnoreCase) ||
                                                     String.Equals(n, "val", StringComparison.OrdinalIgnoreCase))) valueEl = child;
                    }
                    if (nameEl != null)
                    {
                        string key = nameEl.Value == null ? null : nameEl.Value.Trim();
                        string val = valueEl != null ? valueEl.Value : text;
                        AddProperty(props, key, val);
                    }
                }
            }
        }

        private static void HarvestRegex(string xml, Dictionary<string, string> props)
        {
            foreach (KeyValuePair<string, string> item in RegexPatterns)
            {
                try
                {
                    Match m = Regex.Match(xml, item.Value, RegexOptions.IgnoreCase);
                    if (!m.Success) continue;
                    string v = m.Groups[1].Value.Trim();
                    if (v.Length > 0 && !props.ContainsKey(item.Key)) props[item.Key] = v;
                }
                catch { /* Keep parser resilient. */ }
            }
        }

        private static string Find(Dictionary<string, string> props, string[] needles)
        {
            foreach (string needle in needles)
            {
                foreach (KeyValuePair<string, string> kv in props)
                {
                    if (String.Equals(kv.Key, needle, StringComparison.OrdinalIgnoreCase) ||
                        String.Equals(kv.Key, "P_ARTICLE_" + needle, StringComparison.OrdinalIgnoreCase))
                        return Clean(kv.Value);
                }
            }
            foreach (string needle in needles)
            {
                if (needle.Length < 4) continue;
                foreach (KeyValuePair<string, string> kv in props)
                {
                    if (kv.Key.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                        return Clean(kv.Value);
                }
            }
            return null;
        }

        private static string DirectText(XElement el)
        {
            if (el.HasElements) return null;
            string v = el.Value;
            if (String.IsNullOrWhiteSpace(v)) return null;
            v = v.Trim();
            return v.Length > MaxStoredValueLength ? v.Substring(0, MaxStoredValueLength) : v;
        }

        private static void AddProperty(Dictionary<string, string> props, string key, string value)
        {
            if (String.IsNullOrWhiteSpace(key) || String.IsNullOrWhiteSpace(value)) return;
            if (props.Count >= MaxProperties) return;
            key = key.Trim();
            value = value.Trim();
            if (key.Length > 200) return;
            if (value.Length > MaxStoredValueLength) value = value.Substring(0, MaxStoredValueLength);
            if (!props.ContainsKey(key)) props[key] = value;
            string normalized = Normalize(key);
            if (normalized.Length > 0 && !props.ContainsKey(normalized)) props[normalized] = value;
        }

        private static string Normalize(string key)
        {
            StringBuilder sb = new StringBuilder(key.Length);
            foreach (char c in key)
                if (Char.IsLetterOrDigit(c)) sb.Append(Char.ToUpperInvariant(c));
            return sb.ToString();
        }

        private static string Clean(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return null;
            string v = value.Trim();
            if (v.Length > MaxFieldLength) v = v.Substring(0, MaxFieldLength);
            return v;
        }
    }
}
