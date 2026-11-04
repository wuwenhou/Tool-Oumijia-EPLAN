using System;
using System.Text;
using Oumijia.Core.Edz;
using Oumijia.Core.Util;

namespace Oumijia.Core.Classification;

public enum Confidence
{
    Low,
    Medium,
    High
}

public sealed class ClassificationResult
{
    public string ManufacturerFolder { get; set; } = "";
    public string? ManufacturerRaw { get; set; }
    public string CategoryFolder { get; set; } = "";
    public string CategoryReason { get; set; } = "";
    public Confidence ManufacturerConfidence { get; set; } = Confidence.Low;
    public Confidence CategoryConfidence { get; set; } = Confidence.Low;

    /// <summary>E.g. "Siemens/MCB".</summary>
    public string SuggestedRelativeFolder()
    {
        if (string.IsNullOrEmpty(ManufacturerFolder)) return CategoryFolder;
        if (string.IsNullOrEmpty(CategoryFolder)) return ManufacturerFolder;
        return ManufacturerFolder + "/" + CategoryFolder;
    }
}

/// <summary>
/// Rule-based classifier: decides the library folder ("Manufacturer/Category")
/// for an EDZ/EMA file. Only factual metadata is used
/// (manufacturer, product group, designation, part number, file name).
/// </summary>
public sealed class Classifier
{
    private static readonly string[] CompanySuffixes =
    {
        "gmbh & co. kg", "gmbh & co.kg", "gmbh & co", "co. kg", "co kg",
        "co., ltd.", "co.,ltd.", "co., ltd", "co.,ltd", "pte. ltd.", "pte ltd",
        "s.p.a.", "s.a.", "s.r.l.", "srl", "b.v.", "n.v.",
        "corporation", "corp.", "corp", "company", "ltd.", "ltd", "inc.", "inc",
        "llc", "ag", "bv", "nv", "plc"
    };

    private readonly ClassifierRules _rules;

    public Classifier(ClassifierRules rules)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
    }

    public ClassifierRules Rules => _rules;

    public ClassificationResult Classify(string? filePath, EdzPackageInfo? info)
    {
        var fileName = string.IsNullOrEmpty(filePath) ? "" : System.IO.Path.GetFileName(filePath);
        var result = new ClassificationResult();

        // --- Manufacturer ---------------------------------------------------
        var raw = info?.PrimaryManufacturer;
        if (!string.IsNullOrWhiteSpace(raw))
        {
            result.ManufacturerRaw = raw!.Trim();
            result.ManufacturerConfidence = Confidence.High;
        }

        result.ManufacturerFolder = NormalizeManufacturer(result.ManufacturerRaw);

        // --- Category --------------------------------------------------------
        var haystack = BuildHaystack(info, fileName);

        var matched = false;
        foreach (var rule in _rules.Categories)
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.Folder)) continue;

            if (rule.Keywords != null)
            {
                foreach (var keyword in rule.Keywords)
                {
                    if (string.IsNullOrWhiteSpace(keyword)) continue;
                    var kw = keyword.Trim().ToLowerInvariant();
                    if (haystack.IndexOf(kw, StringComparison.Ordinal) >= 0)
                    {
                        result.CategoryFolder = PathUtil.SanitizeFolderName(rule.Folder);
                        result.CategoryReason = "keyword '" + keyword.Trim() + "'";
                        result.CategoryConfidence = kw.Length >= 5 ? Confidence.High : Confidence.Medium;
                        matched = true;
                        break;
                    }
                }
            }

            if (!matched && rule.EClassPrefixes != null)
            {
                var eclass = NormalizeEClass(info?.PrimaryEClass);
                if (eclass.Length > 0)
                {
                    foreach (var prefix in rule.EClassPrefixes)
                    {
                        if (string.IsNullOrWhiteSpace(prefix)) continue;
                        var normalized = NormalizeEClass(prefix);
                        if (normalized.Length > 0 && eclass.StartsWith(normalized, StringComparison.Ordinal))
                        {
                            result.CategoryFolder = PathUtil.SanitizeFolderName(rule.Folder);
                            result.CategoryReason = "eCl@ss " + prefix;
                            result.CategoryConfidence = Confidence.High;
                            matched = true;
                            break;
                        }
                    }
                }
            }

            if (matched) break;
        }

        if (!matched)
        {
            result.CategoryFolder = PathUtil.SanitizeFolderName(_rules.FallbackCategory);
            if (result.CategoryFolder.Length == 0) result.CategoryFolder = "_Unsorted";
            result.CategoryReason = "không khớp quy tắc nào";
            result.CategoryConfidence = Confidence.Low;
        }

        return result;
    }

    public string NormalizeManufacturer(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return SanitizedFallback(_rules.FallbackManufacturer, "_Unknown");

        var trimmed = raw!.Trim();

        if (_rules.ManufacturerAliases != null &&
            _rules.ManufacturerAliases.TryGetValue(trimmed, out var alias) &&
            !string.IsNullOrWhiteSpace(alias))
        {
            return PathUtil.SanitizeFolderName(alias);
        }

        var cleaned = trimmed;
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var suffix in CompanySuffixes)
            {
                if (cleaned.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = cleaned.Substring(0, cleaned.Length - suffix.Length).Trim();
                    cleaned = cleaned.TrimEnd(',', '.', '-', ' ');
                    changed = true;
                    break;
                }
            }
        }

        if (cleaned.Length == 0) cleaned = trimmed;
        return SanitizedFallback(cleaned, "_Unknown");
    }

    private static string SanitizedFallback(string value, string fallback)
    {
        var clean = PathUtil.SanitizeFolderName(value);
        if (clean.Length == 0) clean = fallback;
        return clean;
    }

    private static string NormalizeEClass(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "";
        var value = code!;
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (c >= '0' && c <= '9') sb.Append(c);
        }
        return sb.ToString();
    }

    private static string BuildHaystack(EdzPackageInfo? info, string fileName)
    {
        var sb = new StringBuilder();

        if (info != null)
        {
            if (info.PrimaryProductGroup != null) sb.Append(info.PrimaryProductGroup).Append(' ');
            if (info.PrimaryPartNumber != null) sb.Append(info.PrimaryPartNumber).Append(' ');
            if (info.Parts != null)
            {
                foreach (var part in info.Parts)
                {
                    if (part == null) continue;
                    if (part.Designation != null) sb.Append(part.Designation).Append(' ');
                    if (part.ProductGroup != null) sb.Append(part.ProductGroup).Append(' ');
                }
            }
        }

        sb.Append(fileName);
        return sb.ToString().ToLowerInvariant();
    }
}
