using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Oumijia.Core.Edz;

/// <summary>Portable, factual part data extracted from a part XML inside an EDZ.</summary>
public sealed class EdzPartInfo
{
    public string? PartNumber { get; set; }
    public string? TypeNumber { get; set; }
    public string? OrderNumber { get; set; }
    public string? Designation { get; set; }
    public string? Manufacturer { get; set; }
    public string? ProductGroup { get; set; }
    public string? EClassCode { get; set; }
    public string? SourceXmlEntry { get; set; }

    /// <summary>All harvested raw properties (key =&gt; value), for advanced inspection.</summary>
    public Dictionary<string, string> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Everything Tool Oumijia extracts from one EDZ package.</summary>
public sealed class EdzPackageInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName => Path.GetFileName(FilePath);
    public long FileSizeBytes { get; set; }
    public string ContainerKind { get; set; } = "unknown";
    public string? ManifestVersion { get; set; }

    public List<EdzPartInfo> Parts { get; } = new();
    public List<string> MacroEntries { get; } = new();
    public List<string> DocumentEntries { get; } = new();
    public List<string> DxfEntries { get; } = new();
    public List<string> ImageEntries { get; } = new();
    public List<string> OtherDataEntries { get; } = new();

    public bool HasMacros => MacroEntries.Count > 0;

    public string? PrimaryManufacturer
    {
        get
        {
            foreach (var p in Parts)
                if (!string.IsNullOrWhiteSpace(p.Manufacturer)) return p.Manufacturer;
            return null;
        }
    }

    public string? PrimaryPartNumber
    {
        get
        {
            foreach (var p in Parts)
            {
                if (!string.IsNullOrWhiteSpace(p.PartNumber)) return p.PartNumber;
                if (!string.IsNullOrWhiteSpace(p.TypeNumber)) return p.TypeNumber;
                if (!string.IsNullOrWhiteSpace(p.OrderNumber)) return p.OrderNumber;
            }
            return null;
        }
    }

    public string? PrimaryProductGroup
    {
        get
        {
            foreach (var p in Parts)
                if (!string.IsNullOrWhiteSpace(p.ProductGroup)) return p.ProductGroup;
            return null;
        }
    }

    public string? PrimaryDesignation
    {
        get
        {
            foreach (var p in Parts)
                if (!string.IsNullOrWhiteSpace(p.Designation)) return p.Designation;
            return null;
        }
    }

    public string? PrimaryEClass
    {
        get
        {
            foreach (var p in Parts)
                if (!string.IsNullOrWhiteSpace(p.EClassCode)) return p.EClassCode;
            return null;
        }
    }

    /// <summary>Short human-readable description, e.g. "2 part(s), 1 macro(s), 1 DXF".</summary>
    public string Summary()
    {
        var sb = new StringBuilder();
        sb.Append(Parts.Count).Append(" part");
        if (Parts.Count != 1) sb.Append('s');
        if (MacroEntries.Count > 0) sb.Append(", ").Append(MacroEntries.Count).Append(" macro");
        if (MacroEntries.Count > 1) sb.Append('s');
        if (DxfEntries.Count > 0) sb.Append(", ").Append(DxfEntries.Count).Append(" DXF");
        if (DocumentEntries.Count > 0) sb.Append(", ").Append(DocumentEntries.Count).Append(" doc");
        if (DocumentEntries.Count > 1) sb.Append('s');
        if (ImageEntries.Count > 0) sb.Append(", ").Append(ImageEntries.Count).Append(" image");
        if (ImageEntries.Count > 1) sb.Append('s');
        return sb.ToString();
    }
}
