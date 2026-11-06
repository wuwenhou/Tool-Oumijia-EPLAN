using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Oumijia.EdzReader
{
    /// <summary>Portable, factual part data extracted from a part XML inside an EDZ.</summary>
    public sealed class EdzPartInfo
    {
        public string PartNumber { get; set; }
        public string TypeNumber { get; set; }
        public string OrderNumber { get; set; }
        public string Designation { get; set; }
        public string Manufacturer { get; set; }
        public string ProductGroup { get; set; }
        public string EClassCode { get; set; }
        public string SourceXmlEntry { get; set; }

        /// <summary>All harvested raw properties (key => value), for advanced inspection.</summary>
        public Dictionary<string, string> Properties { get; private set; }

        public EdzPartInfo()
        {
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>Everything Tool Oumijia extracts from one EDZ package.</summary>
    public sealed class EdzPackageInfo
    {
        public string FilePath { get; set; }
        public long FileSizeBytes { get; set; }
        public string ContainerKind { get; set; }
        public string ManifestVersion { get; set; }

        public List<EdzPartInfo> Parts { get; private set; }
        public List<string> MacroEntries { get; private set; }
        public List<string> DocumentEntries { get; private set; }
        public List<string> DxfEntries { get; private set; }
        public List<string> ImageEntries { get; private set; }
        public List<string> OtherDataEntries { get; private set; }

        public string FileName
        {
            get { return String.IsNullOrWhiteSpace(FilePath) ? String.Empty : Path.GetFileName(FilePath); }
        }

        public bool HasMacros
        {
            get { return MacroEntries.Count > 0; }
        }

        public string PrimaryManufacturer
        {
            get
            {
                foreach (EdzPartInfo p in Parts)
                    if (!String.IsNullOrWhiteSpace(p.Manufacturer)) return p.Manufacturer;
                return null;
            }
        }

        public string PrimaryPartNumber
        {
            get
            {
                foreach (EdzPartInfo p in Parts)
                {
                    if (!String.IsNullOrWhiteSpace(p.PartNumber)) return p.PartNumber;
                    if (!String.IsNullOrWhiteSpace(p.TypeNumber)) return p.TypeNumber;
                    if (!String.IsNullOrWhiteSpace(p.OrderNumber)) return p.OrderNumber;
                }
                return null;
            }
        }

        public string PrimaryProductGroup
        {
            get
            {
                foreach (EdzPartInfo p in Parts)
                    if (!String.IsNullOrWhiteSpace(p.ProductGroup)) return p.ProductGroup;
                return null;
            }
        }

        public string PrimaryDesignation
        {
            get
            {
                foreach (EdzPartInfo p in Parts)
                    if (!String.IsNullOrWhiteSpace(p.Designation)) return p.Designation;
                return null;
            }
        }

        public string PrimaryEClass
        {
            get
            {
                foreach (EdzPartInfo p in Parts)
                    if (!String.IsNullOrWhiteSpace(p.EClassCode)) return p.EClassCode;
                return null;
            }
        }

        public EdzPackageInfo()
        {
            FilePath = String.Empty;
            FileSizeBytes = 0;
            ContainerKind = "unknown";
            Parts = new List<EdzPartInfo>();
            MacroEntries = new List<string>();
            DocumentEntries = new List<string>();
            DxfEntries = new List<string>();
            ImageEntries = new List<string>();
            OtherDataEntries = new List<string>();
        }

        /// <summary>Short human-readable description, e.g. "2 part(s), 1 macro(s)".</summary>
        public string Summary()
        {
            StringBuilder sb = new StringBuilder();
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
}
