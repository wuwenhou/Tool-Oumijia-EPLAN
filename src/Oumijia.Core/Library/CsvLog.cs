using System;
using System.IO;
using System.Text;

namespace Oumijia.Core.Library;

/// <summary>Simple CSV log writer (UTF-8 with BOM so Excel opens it correctly).</summary>
public sealed class CsvLog : IDisposable
{
    private readonly StreamWriter _writer;
    private bool _disposed;

    public string FilePath { get; }

    public CsvLog(string filePath, string[] headers)
    {
        FilePath = filePath;
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        _writer = new StreamWriter(filePath, false, new UTF8Encoding(true));
        var sb = new StringBuilder();
        for (var i = 0; i < headers.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(Escape(headers[i]));
        }
        _writer.WriteLine(sb.ToString());
        _writer.Flush();
    }

    public void Append(params string?[] fields)
    {
        if (_disposed) return;

        var sb = new StringBuilder();
        for (var i = 0; i < fields.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(Escape(fields[i]));
        }
        _writer.WriteLine(sb.ToString());
        _writer.Flush();
    }

    private static string Escape(string? value)
    {
        var v = value ?? "";
        if (v.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
        {
            return "\"" + v.Replace("\"", "\"\"") + "\"";
        }
        return v;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            _writer.Flush();
            _writer.Dispose();
        }
        catch
        {
            // Logging must never crash the app.
        }
    }
}
