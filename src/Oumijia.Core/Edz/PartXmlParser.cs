using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Oumijia.Core.Edz;

/// <summary>
/// Tolerant reader for EPLAN part XML found inside EDZ packages.
///
/// There is no guaranteed formal schema for every producer: packages in the wild
/// use different tag styles (e.g. &lt;property id="P_ARTICLE_MANUFACTURER"&gt;Siemens&lt;/property&gt;
/// or plain &lt;Manufacturer&gt;Siemens&lt;/Manufacturer&gt;). This parser therefore harvests
/// (key =&gt; value) pairs from element names and common attributes (id / name / key),
/// then looks up the well-known EPLAN property keys with a fuzzy match.
/// </summary>
public static class PartXmlParser
{
    private const int MaxProperties = 4000;
    private const int MaxStoredValueLength = 4000;
    private const int MaxFieldLength = 300;

    private static readonly (string Key, string Pattern)[] RegexPatterns =
    {
        ("P_ARTICLE_MANUFACTURER", @"P[_\-. ]?ARTICLE[_\-. ]?MANUFACTURER[^>]{0,150}>\s*([^<]{1,200})<"),
        ("P_ARTICLE_PARTNR", @"P[_\-. ]?ARTICLE[_\-. ]?PARTNR[^>]{0,150}>\s*([^<]{1,200})<"),
        ("P_ARTICLE_TYPENR", @"P[_\-. ]?ARTICLE[_\-. ]?TYPENR[^>]{0,150}>\s*([^<]{1,200})<"),
        ("P_ARTICLE_ORDERNR", @"P[_\-. ]?ARTICLE[_\-. ]?ORDERNR[^>]{0,150}>\s*([^<]{1,200})<"),
        ("MANUFACTURER", @"<[^>]{0,60}MANUFACTURER[^>]{0,60}>\s*([^<]{1,200})<"),
        ("PARTNUMBER", @"<[^>]{0,60}PART[_\s]?NUMBER[^>]{0,60}>\s*([^<]{1,200})<"),
    };

    public static EdzPartInfo? Parse(string xmlText, string sourceEntry)
    {
        if (string.IsNullOrWhiteSpace(xmlText)) return null;

        var props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            HarvestXml(xmlText, props);
        }
        catch
        {
            // Invalid XML: fall through to the regex harvest below.
        }

        if (props.Count == 0) HarvestRegex(xmlText, props);

        var part = new EdzPartInfo { SourceXmlEntry = sourceEntry };
        foreach (var kv in props) part.Properties[kv.Key] = kv.Value;

        part.Manufacturer = Find(props, "MANUFACTURER", "HERSTELLER", "MAKER");
        part.PartNumber = Find(props, "PARTNR", "PARTNUMBER", "PART_NO", "ARTIKELNR");
        part.TypeNumber = Find(props, "TYPENR", "TYPENUMBER", "TYPEDESIGNATION");
        part.OrderNumber = Find(props, "ORDERNR", "ORDERNUMBER", "BESTELLNR");
        part.Designation = Find(props, "DESIGNATION", "DESCR1", "DESCRIPTION", "BENENNUNG");
        part.ProductGroup = Find(props, "PRODUCTTOPGROUP", "PRODUCTGROUP", "PRODUCTSUBGROUP", "WARENGRUPPE");
        part.EClassCode = Find(props, "ECLASS");

        if (string.IsNullOrEmpty(part.PartNumber) &&
            string.IsNullOrEmpty(part.TypeNumber) &&
            string.IsNullOrEmpty(part.Manufacturer))
        {
            return null;
        }

        return part;
    }

    private static void HarvestXml(string xml, Dictionary<string, string> props)
    {
        var doc = XDocument.Parse(xml, LoadOptions.None);
        var root = doc.Root;
        if (root == null) return;

        foreach (var el in root.DescendantsAndSelf())
        {
            if (props.Count >= MaxProperties) return;

            var text = DirectText(el);
            AddProperty(props, el.Name.LocalName, text);

            foreach (var attr in el.Attributes())
            {
                var attrName = attr.Name.LocalName;
                if (attrName.Equals("id", StringComparison.OrdinalIgnoreCase) ||
                    attrName.Equals("name", StringComparison.OrdinalIgnoreCase) ||
                    attrName.Equals("key", StringComparison.OrdinalIgnoreCase))
                {
                    AddProperty(props, attr.Value, text);
                }
            }

            if (el.HasElements)
            {
                XElement? nameEl = null;
                XElement? valueEl = null;
                foreach (var child in el.Elements())
                {
                    var n = child.Name.LocalName;
                    if (nameEl == null && (n.Equals("name", StringComparison.OrdinalIgnoreCase) ||
                                           n.Equals("key", StringComparison.OrdinalIgnoreCase) ||
                                           n.Equals("id", StringComparison.OrdinalIgnoreCase)))
                    {
                        nameEl = child;
                    }
                    else if (valueEl == null && (n.Equals("value", StringComparison.OrdinalIgnoreCase) ||
                                                 n.Equals("val", StringComparison.OrdinalIgnoreCase)))
                    {
                        valueEl = child;
                    }
                }

                if (nameEl != null)
                {
                    var key = nameEl.Value?.Trim();
                    var val = valueEl != null ? valueEl.Value : text;
                    AddProperty(props, key, val);
                }
            }
        }
    }

    private static void HarvestRegex(string xml, Dictionary<string, string> props)
    {
        foreach (var (key, pattern) in RegexPatterns)
        {
            try
            {
                var m = Regex.Match(xml, pattern, RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var v = m.Groups[1].Value.Trim();
                    if (v.Length > 0 && !props.ContainsKey(key)) props[key] = v;
                }
            }
            catch
            {
                // Malformed pattern should never happen; keep the parser resilient.
            }
        }
    }

    private static string? Find(Dictionary<string, string> props, params string[] needles)
    {
        // Exact match pass: direct name or the usual P_ARTICLE_ prefix.
        foreach (var needle in needles)
        {
            foreach (var kv in props)
            {
                if (kv.Key.Equals(needle, StringComparison.OrdinalIgnoreCase) ||
                    kv.Key.Equals("P_ARTICLE_" + needle, StringComparison.OrdinalIgnoreCase))
                {
                    return Clean(kv.Value);
                }
            }
        }

        // Fuzzy pass: any key containing the needle (normalized keys included).
        foreach (var needle in needles)
        {
            if (needle.Length < 4) continue;
            foreach (var kv in props)
            {
                if (kv.Key.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                    return Clean(kv.Value);
            }
        }

        return null;
    }

    private static string? DirectText(XElement el)
    {
        if (el.HasElements) return null;
        var v = el.Value;
        if (string.IsNullOrWhiteSpace(v)) return null;
        v = v.Trim();
        return v.Length > MaxStoredValueLength ? v.Substring(0, MaxStoredValueLength) : v;
    }

    private static void AddProperty(Dictionary<string, string> props, string? key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value)) return;
        if (props.Count >= MaxProperties) return;

        key = key!.Trim();
        value = value!.Trim();
        if (key.Length > 200) return;
        if (value.Length > MaxStoredValueLength) value = value.Substring(0, MaxStoredValueLength);

        if (!props.ContainsKey(key)) props[key] = value;

        var normalized = Normalize(key);
        if (normalized.Length > 0 && !props.ContainsKey(normalized)) props[normalized] = value;
    }

    private static string Normalize(string key)
    {
        var sb = new StringBuilder(key.Length);
        foreach (var c in key)
        {
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToUpperInvariant(c));
        }
        return sb.ToString();
    }

    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value!.Trim();
        if (v.Length > MaxFieldLength) v = v.Substring(0, MaxFieldLength);
        return v;
    }
}
