using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using Oumijia.Core.Edz;
using Xunit;

namespace Oumijia.Core.Tests;

/// <summary>
/// Verifies that real 7z-format EDZ containers are readable. Requires a local
/// 7-Zip installation to *create* the fixture; on machines without 7-Zip the
/// test passes trivially (skip-by-return) so CI stays portable.
/// </summary>
public class SevenZipTests : IDisposable
{
    private static readonly string[] SevenZipCandidates =
    {
        @"C:\Program Files\7-Zip\7z.exe",
        @"C:\Program Files (x86)\7-Zip\7z.exe"
    };

    private readonly string _dir = TestFixtures.CreateTempDir();

    [Fact]
    public void Reads_7z_edz_container_when_7zip_is_available()
    {
        var sevenZip = FindSevenZip();
        if (sevenZip == null) return;

        var zipEdz = TestFixtures.CreateEdz(_dir, "source.edz", "ABB", "S7Z-RELAY-1", "Interface relay");
        var extractDir = Path.Combine(_dir, "content");
        ZipFile.ExtractToDirectory(zipEdz, extractDir);

        var sevenZipEdz = Path.Combine(_dir, "sevenzip.edz");
        var exitCode = RunSevenZip(sevenZip, "a -t7z \"" + sevenZipEdz + "\" \".\\*\"", extractDir);
        Assert.Equal(0, exitCode);

        var info = EdzPackageReader.Read(sevenZipEdz);

        Assert.Equal("7z", info.ContainerKind);
        Assert.Equal("ABB", info.PrimaryManufacturer);
        Assert.Equal("S7Z-RELAY-1", info.PrimaryPartNumber);
        Assert.Single(info.MacroEntries);
    }

    private static int RunSevenZip(string exe, string arguments, string workingDirectory)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(psi);
        if (process == null) return -1;

        process.StandardOutput.ReadToEnd();
        process.StandardError.ReadToEnd();
        process.WaitForExit(60000);
        return process.HasExited ? process.ExitCode : -1;
    }

    private static string? FindSevenZip()
    {
        foreach (var candidate in SevenZipCandidates)
        {
            try
            {
                if (File.Exists(candidate)) return candidate;
            }
            catch
            {
                // Keep probing.
            }
        }
        return null;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, true);
        }
        catch
        {
            // Best-effort cleanup.
        }
    }
}
