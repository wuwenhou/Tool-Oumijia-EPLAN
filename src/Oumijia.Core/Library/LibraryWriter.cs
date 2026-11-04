using System;
using System.IO;
using Oumijia.Core.Util;

namespace Oumijia.Core.Library;

public enum ConflictPolicy
{
    /// <summary>Keep both: the new file gets " (2)", " (3)"... (default, safest).</summary>
    Rename,

    /// <summary>Keep the existing file, skip the new one.</summary>
    Skip,

    /// <summary>Replace the existing file.</summary>
    Overwrite
}

public enum LibraryWriteStatus
{
    New,
    Renamed,
    Overwritten,
    SkippedIdentical,
    SkippedExisting,
    Error
}

public sealed class LibraryWriteResult
{
    public LibraryWriteStatus Status { get; set; }
    public string SourcePath { get; set; } = "";
    public string DestinationPath { get; set; } = "";
    public string Message { get; set; } = "";
    public string? Sha256 { get; set; }

    public string StatusText
    {
        get
        {
            switch (Status)
            {
                case LibraryWriteStatus.New: return "Đã thêm mới";
                case LibraryWriteStatus.Renamed: return "Đổi tên (trùng)";
                case LibraryWriteStatus.Overwritten: return "Ghi đè";
                case LibraryWriteStatus.SkippedIdentical: return "Bỏ qua (trùng nội dung)";
                case LibraryWriteStatus.SkippedExisting: return "Bỏ qua (đã tồn tại)";
                default: return "Lỗi";
            }
        }
    }
}

/// <summary>
/// Files an input file into a managed library tree:
/// root /&lt;relative folder&gt;/&lt;file name&gt;, with safe conflict handling.
/// Reused both for the EDZ library and for EPLAN macro installation.
/// </summary>
public sealed class LibraryWriter
{
    private readonly string _root;
    private readonly ConflictPolicy _policy;
    private readonly bool _move;

    public LibraryWriter(string root, ConflictPolicy policy, bool move)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Library root is required.", nameof(root));

        _root = root;
        _policy = policy;
        _move = move;
    }

    public string Root => _root;

    public LibraryWriteResult Write(string sourcePath, string? relativeFolder)
    {
        var result = new LibraryWriteResult { SourcePath = sourcePath };

        try
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                result.Status = LibraryWriteStatus.Error;
                result.Message = "Không tìm thấy tệp nguồn.";
                return result;
            }

            var destinationDirectory = PathUtil.CombineRelative(_root, relativeFolder);
            Directory.CreateDirectory(destinationDirectory);

            var fileName = Path.GetFileName(sourcePath);
            var destinationPath = Path.Combine(destinationDirectory, fileName);

            var fullSource = Path.GetFullPath(sourcePath);
            var fullDestination = Path.GetFullPath(destinationPath);
            if (string.Equals(fullSource, fullDestination, StringComparison.OrdinalIgnoreCase))
            {
                result.Status = LibraryWriteStatus.SkippedIdentical;
                result.DestinationPath = destinationPath;
                result.Message = "Tệp nguồn đã nằm đúng vị trí đích.";
                return result;
            }

            if (File.Exists(destinationPath))
            {
                if (Hashing.FilesAreIdentical(sourcePath, destinationPath))
                {
                    result.Status = LibraryWriteStatus.SkippedIdentical;
                    result.DestinationPath = destinationPath;
                    result.Message = "Tệp đích giống hệt nội dung (bỏ qua).";
                    result.Sha256 = Hashing.TryComputeSha256(destinationPath);
                    return result;
                }

                switch (_policy)
                {
                    case ConflictPolicy.Skip:
                        result.Status = LibraryWriteStatus.SkippedExisting;
                        result.DestinationPath = destinationPath;
                        result.Message = "Đã tồn tại tệp khác nội dung (bỏ qua).";
                        return result;

                    case ConflictPolicy.Overwrite:
                        result.Status = LibraryWriteStatus.Overwritten;
                        break;

                    default:
                        destinationPath = PathUtil.UniqueDestination(destinationDirectory, fileName);
                        fullDestination = Path.GetFullPath(destinationPath);
                        result.Status = LibraryWriteStatus.Renamed;
                        break;
                }
            }
            else
            {
                result.Status = LibraryWriteStatus.New;
            }

            if (_move)
            {
                if (File.Exists(destinationPath)) File.Delete(destinationPath);
                File.Move(fullSource, fullDestination);
            }
            else
            {
                File.Copy(fullSource, fullDestination, true);
            }

            result.DestinationPath = destinationPath;
            result.Sha256 = Hashing.TryComputeSha256(destinationPath);
            result.Message = BuildMessage(result.Status, _move, Path.GetFileName(destinationPath));
            return result;
        }
        catch (Exception ex)
        {
            result.Status = LibraryWriteStatus.Error;
            result.Message = ex.Message;
            return result;
        }
    }

    private static string BuildMessage(LibraryWriteStatus status, bool moved, string finalFileName)
    {
        var verb = moved ? "Đã di chuyển" : "Đã sao chép";
        switch (status)
        {
            case LibraryWriteStatus.Overwritten:
                return verb + " (ghi đè tệp cũ).";
            case LibraryWriteStatus.Renamed:
                return verb + " với tên mới: " + finalFileName;
            default:
                return verb + ".";
        }
    }
}
