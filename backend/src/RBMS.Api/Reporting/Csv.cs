using System.Globalization;
using System.Text;

namespace RBMS.Api.Reporting;

/// <summary>Minimal, dependency-free RFC 4180 CSV writer for report exports.</summary>
public static class Csv
{
    public static byte[] Build(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(Escape)));
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", row.Select(Escape)));
        // UTF-8 BOM so Excel opens it with correct encoding.
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    public static string Num(decimal value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Escape(string? field)
    {
        var s = field ?? "";
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }
}
