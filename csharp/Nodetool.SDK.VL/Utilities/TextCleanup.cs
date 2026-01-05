namespace Nodetool.SDK.VL.Utilities;

internal static class TextCleanup
{
    /// <summary>
    /// Removes a single trailing period from a line (but keeps ellipses "...").
    /// Also trims trailing whitespace.
    /// </summary>
    public static string StripTrailingPeriod(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        var s = text.TrimEnd();
        if (s.EndsWith(".", StringComparison.Ordinal) && !s.EndsWith("..", StringComparison.Ordinal))
            s = s.Substring(0, s.Length - 1);
        return s;
    }

    /// <summary>
    /// Applies StripTrailingPeriod to each line of a multi-line string.
    /// </summary>
    public static string StripTrailingPeriodsPerLine(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        var lines = text.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
            lines[i] = StripTrailingPeriod(lines[i]);
        return string.Join("\n", lines);
    }
}


