namespace RBMS.Application.Features.Documents;

/// <summary>Tags are stored as a normalized, comma-separated, lowercase string and exposed as an array.</summary>
public static class DocumentTags
{
    /// <summary>"GST, Legal ,gst" → "gst,legal" (trimmed, lowercased, de-duplicated, order preserved).</summary>
    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var tags = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .ToList();
        return tags.Count == 0 ? null : string.Join(',', tags);
    }

    public static IReadOnlyList<string> Split(string? stored) =>
        string.IsNullOrWhiteSpace(stored)
            ? Array.Empty<string>()
            : stored.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
