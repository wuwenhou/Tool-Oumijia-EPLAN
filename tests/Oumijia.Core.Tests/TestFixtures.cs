using System;
using System.IO;
using System.IO.Compression;

namespace Oumijia.Core.Tests;

/// <summary>
/// Creates synthetic EDZ fixtures (ZIP containers — the same layout as real
/// packages: manifest.xml + items/partxml/*.part.xml + attachments).
/// Synthetic data only; no vendor content is used anywhere in this repository.
/// </summary>
internal static class TestFixtures
{
    public static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "oumijia-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static string CreateEdz(
        string directory,
        string fileName,
        string manufacturer,
        string partNumber,
        string productGroup,
        string? designation = null)
    {
        var path = Path.Combine(directory, fileName);
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        AddText(zip, "manifest.xml", "<manifest version=\"2.0\"><items/></manifest>");
        AddText(zip, $"items/partxml/{partNumber}.part.xml",
            PartXmlPropertyStyle(manufacturer, partNumber, productGroup, designation ?? productGroup));
        AddText(zip, $"macros/{partNumber}.ema", "EMA-DUMMY-CONTENT");
        AddText(zip, "pictures/preview.png", "PNG-DUMMY");
        return path;
    }

    public static string CreateEdzPlainTagStyle(
        string directory,
        string fileName,
        string manufacturer,
        string partNumber,
        string productGroup)
    {
        var path = Path.Combine(directory, fileName);
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        AddText(zip, "manifest.xml", "<manifest version=\"2.0\"/>");
        AddText(zip, "PartData.xml",
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<EplanPartData>\n" +
            $"  <Manufacturer>{Escape(manufacturer)}</Manufacturer>\n" +
            $"  <PartNumber>{Escape(partNumber)}</PartNumber>\n" +
            $"  <ProductGroup>{Escape(productGroup)}</ProductGroup>\n" +
            "</EplanPartData>");
        return path;
    }

    public static string CreateEmptyEdz(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        AddText(zip, "manifest.xml", "<manifest version=\"2.0\"/>");
        return path;
    }

    private static string PartXmlPropertyStyle(string manufacturer, string partNumber, string productGroup, string designation)
    {
        return "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
               "<part>\n" +
               $"  <property id=\"P_ARTICLE_MANUFACTURER\">{Escape(manufacturer)}</property>\n" +
               $"  <property id=\"P_ARTICLE_PARTNR\">{Escape(partNumber)}</property>\n" +
               $"  <property id=\"P_ARTICLE_PRODUCTGROUP\">{Escape(productGroup)}</property>\n" +
               $"  <property id=\"P_ARTICLE_DESCR1\">{Escape(designation)}</property>\n" +
               "</part>";
    }

    private static void AddText(ZipArchive zip, string entryName, string content)
    {
        var entry = zip.CreateEntry(entryName);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream);
        writer.Write(content);
    }

    private static string Escape(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
