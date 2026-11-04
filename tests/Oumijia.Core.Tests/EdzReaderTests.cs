using System;
using System.IO;
using System.IO.Compression;
using Oumijia.Core.Edz;
using Xunit;

namespace Oumijia.Core.Tests;

public class EdzReaderTests : IDisposable
{
    private readonly string _dir = TestFixtures.CreateTempDir();

    [Fact]
    public void Reads_metadata_from_property_style_part_xml()
    {
        var path = TestFixtures.CreateEdz(_dir, "ABB_S201-C16.edz", "ABB", "S201-C16", "Miniature circuit breaker");

        var info = EdzPackageReader.Read(path);

        Assert.Equal("zip", info.ContainerKind);
        Assert.Single(info.Parts);
        Assert.Equal("ABB", info.PrimaryManufacturer);
        Assert.Equal("S201-C16", info.PrimaryPartNumber);
        Assert.NotNull(info.PrimaryProductGroup);
        Assert.True(info.PrimaryProductGroup!.IndexOf("circuit breaker", StringComparison.OrdinalIgnoreCase) >= 0);
        Assert.Single(info.MacroEntries);
        Assert.Single(info.ImageEntries);
        Assert.Equal("2.0", info.ManifestVersion);
    }

    [Fact]
    public void Reads_metadata_from_plain_tag_style_part_xml()
    {
        var path = TestFixtures.CreateEdzPlainTagStyle(_dir, "Siemens_3RT.edz", "Siemens", "3RT2024-1AL20", "Contactor");

        var info = EdzPackageReader.Read(path);

        Assert.Single(info.Parts);
        Assert.Equal("Siemens", info.PrimaryManufacturer);
        Assert.Equal("3RT2024-1AL20", info.PrimaryPartNumber);
        Assert.Equal("Contactor", info.PrimaryProductGroup);
    }

    [Fact]
    public void Returns_empty_model_for_zip_without_part_data()
    {
        var path = TestFixtures.CreateEmptyEdz(_dir, "empty.edz");

        var info = EdzPackageReader.Read(path);

        Assert.Empty(info.Parts);
        Assert.Equal("2.0", info.ManifestVersion);
        Assert.Equal("zip", info.ContainerKind);
    }

    [Fact]
    public void Detects_zip_container_from_magic_bytes()
    {
        var path = TestFixtures.CreateEdz(_dir, "detect.edz", "ABB", "P1", "Relay");
        Assert.Equal("zip", EdzArchive.DetectContainer(path));
    }

    [Fact]
    public void Throws_for_missing_file()
    {
        Assert.Throws<FileNotFoundException>(() => EdzPackageReader.Read(Path.Combine(_dir, "nope.edz")));
    }

    [Fact]
    public void Reads_plain_zip_with_wrong_extension_gracefully()
    {
        // Some producers ship packages that are plain ZIP even when named .edz —
        // and vice versa. The reader goes by content, not by extension.
        var path = Path.Combine(_dir, "oddball.bin");
        using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            var entry = zip.CreateEntry("manifest.xml");
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream);
            writer.Write("<manifest version=\"2.0\"/>");
        }

        var info = EdzPackageReader.Read(path);
        Assert.Equal("zip", info.ContainerKind);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, true);
        }
        catch
        {
            // Test cleanup is best-effort.
        }
    }
}
