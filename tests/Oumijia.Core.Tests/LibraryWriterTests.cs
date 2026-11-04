using System;
using System.IO;
using Oumijia.Core.Library;
using Oumijia.Core.Util;
using Xunit;

namespace Oumijia.Core.Tests;

public class LibraryWriterTests : IDisposable
{
    private readonly string _root = TestFixtures.CreateTempDir();
    private readonly string _sourceDir = TestFixtures.CreateTempDir();

    private string CreateSource(string content)
    {
        var path = Path.Combine(_sourceDir, "sample.edz");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Files_new_file_into_manufacturer_category_tree()
    {
        var source = CreateSource("v1");
        var writer = new LibraryWriter(_root, ConflictPolicy.Rename, move: false);

        var result = writer.Write(source, "ABB/MCB");

        Assert.Equal(LibraryWriteStatus.New, result.Status);
        Assert.True(File.Exists(result.DestinationPath));
        Assert.Equal(Path.Combine(_root, "ABB", "MCB", "sample.edz"), result.DestinationPath);
        Assert.Equal("v1", File.ReadAllText(result.DestinationPath));
    }

    [Fact]
    public void Skips_identical_duplicate()
    {
        var source = CreateSource("same");
        var writer = new LibraryWriter(_root, ConflictPolicy.Rename, move: false);

        writer.Write(source, "ABB/MCB");
        var second = writer.Write(source, "ABB/MCB");

        Assert.Equal(LibraryWriteStatus.SkippedIdentical, second.Status);
    }

    [Fact]
    public void Renames_on_conflict_with_different_content()
    {
        var writer = new LibraryWriter(_root, ConflictPolicy.Rename, move: false);

        var source = CreateSource("v1");
        writer.Write(source, "ABB/MCB");

        File.WriteAllText(source, "v2-different");
        var second = writer.Write(source, "ABB/MCB");

        Assert.Equal(LibraryWriteStatus.Renamed, second.Status);
        Assert.EndsWith("sample (2).edz", second.DestinationPath);
        Assert.Equal("v2-different", File.ReadAllText(second.DestinationPath));
        Assert.Equal("v1", File.ReadAllText(Path.Combine(_root, "ABB", "MCB", "sample.edz")));
    }

    [Fact]
    public void Skips_existing_on_skip_policy()
    {
        var writer = new LibraryWriter(_root, ConflictPolicy.Skip, move: false);

        var source = CreateSource("v1");
        writer.Write(source, "Tenant");

        File.WriteAllText(source, "v2");
        var second = writer.Write(source, "Tenant");

        Assert.Equal(LibraryWriteStatus.SkippedExisting, second.Status);
        Assert.Equal("v1", File.ReadAllText(Path.Combine(_root, "Tenant", "sample.edz")));
    }

    [Fact]
    public void Overwrites_existing_on_overwrite_policy()
    {
        var writer = new LibraryWriter(_root, ConflictPolicy.Overwrite, move: false);

        var source = CreateSource("v1");
        writer.Write(source, "Tenant");

        File.WriteAllText(source, "v2");
        var second = writer.Write(source, "Tenant");

        Assert.Equal(LibraryWriteStatus.Overwritten, second.Status);
        Assert.Equal("v2", File.ReadAllText(Path.Combine(_root, "Tenant", "sample.edz")));
    }

    [Fact]
    public void Moves_source_when_move_enabled()
    {
        var writer = new LibraryWriter(_root, ConflictPolicy.Rename, move: true);
        var source = CreateSource("move-me");

        var result = writer.Write(source, "Vendor/Category");

        Assert.Equal(LibraryWriteStatus.New, result.Status);
        Assert.False(File.Exists(source));
        Assert.True(File.Exists(result.DestinationPath));
    }

    [Fact]
    public void Sanitizes_invalid_folder_characters()
    {
        var source = CreateSource("x");
        var writer = new LibraryWriter(_root, ConflictPolicy.Rename, move: false);

        var result = writer.Write(source, "ACME/Bad:Name*Here");

        Assert.True(File.Exists(result.DestinationPath));
        Assert.Contains("Bad_Name_Here", result.DestinationPath);
    }

    [Fact]
    public void PathUtil_unique_destination_appends_counter()
    {
        var dir = Path.Combine(_root, "uniq");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "a.edz"), "1");

        var unique = PathUtil.UniqueDestination(dir, "a.edz");
        Assert.EndsWith("a (2).edz", unique);
    }

    public void Dispose()
    {
        foreach (var dir in new[] { _root, _sourceDir })
        {
            try
            {
                Directory.Delete(dir, true);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }
}
