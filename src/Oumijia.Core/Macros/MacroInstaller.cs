using System;
using System.Collections.Generic;
using System.IO;
using Oumijia.Core.Library;
using Oumijia.Core.Util;

namespace Oumijia.Core.Macros;

/// <summary>
/// Probes common EPLAN master-data macro locations.
/// The authoritative folder can always be read in EPLAN under
/// Options &gt; Settings &gt; User &gt; Management &gt; Directories (master data paths),
/// and overridden in Tool Oumijia's settings.
/// </summary>
public static class MacroPathDetector
{
    public static List<string> DetectCandidates()
    {
        var found = new List<string>();

        void Probe(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                var expanded = PathUtil.ExpandPath(path);
                if (Directory.Exists(expanded) && !found.Contains(expanded)) found.Add(expanded);
            }
            catch
            {
                // Ignore probe failures.
            }
        }

        var publicRoot = Environment.GetEnvironmentVariable("PUBLIC") ?? "C:\\Users\\Public";
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        var macrosRoot = Path.Combine(publicRoot, "EPLAN", "Data", "Macros");
        Probe(macrosRoot);
        Probe(Path.Combine(publicRoot, "Documents", "EPLAN"));
        Probe(Path.Combine(programData, "EPLAN"));

        // Company sub-folders under the well-known macros root are valid targets too.
        try
        {
            if (Directory.Exists(macrosRoot))
            {
                foreach (var dir in Directory.GetDirectories(macrosRoot))
                {
                    if (!found.Contains(dir)) found.Add(dir);
                }
            }
        }
        catch
        {
            // Best-effort detection.
        }

        return found;
    }

    public static bool IsMacroFile(string path)
    {
        var ext = Path.GetExtension(path);
        return Edz.EdzPackageReader.IsMacroExtension(ext);
    }
}

/// <summary>
/// Installs .ema / .emp / .ems files into an EPLAN master-data macro directory
/// (import option "A": plain file copy, EPLAN picks the macros up on restart).
/// </summary>
public sealed class MacroInstaller
{
    private readonly ConflictPolicy _policy;

    public MacroInstaller(ConflictPolicy policy)
    {
        _policy = policy;
    }

    public LibraryWriteResult Install(string macroFilePath, string targetDirectory)
    {
        if (string.IsNullOrWhiteSpace(macroFilePath) || !File.Exists(macroFilePath))
        {
            return new LibraryWriteResult
            {
                SourcePath = macroFilePath ?? "",
                Status = LibraryWriteStatus.Error,
                Message = "Không tìm thấy tệp macro nguồn."
            };
        }

        if (!MacroPathDetector.IsMacroFile(macroFilePath))
        {
            return new LibraryWriteResult
            {
                SourcePath = macroFilePath,
                Status = LibraryWriteStatus.Error,
                Message = "Không phải tệp macro EPLAN (.ema/.emp/.ems)."
            };
        }

        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            return new LibraryWriteResult
            {
                SourcePath = macroFilePath,
                Status = LibraryWriteStatus.Error,
                Message = "Chưa chọn thư mục macro đích."
            };
        }

        var writer = new LibraryWriter(targetDirectory, _policy, move: false);
        return writer.Write(macroFilePath, null);
    }
}
