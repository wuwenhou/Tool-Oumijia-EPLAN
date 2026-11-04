using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Oumijia.Core.Util;

public static class Hashing
{
    /// <summary>SHA-256 of a file, lowercase hex. Returns null on IO failure.</summary>
    public static string? TryComputeSha256(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(stream);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
        catch
        {
            return null;
        }
    }

    public static bool FilesAreIdentical(string left, string right)
    {
        try
        {
            var li = new FileInfo(left);
            var ri = new FileInfo(right);
            if (!li.Exists || !ri.Exists) return false;
            if (li.Length != ri.Length) return false;

            var lh = TryComputeSha256(left);
            var rh = TryComputeSha256(right);
            return lh != null && rh != null && lh.Equals(rh, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
