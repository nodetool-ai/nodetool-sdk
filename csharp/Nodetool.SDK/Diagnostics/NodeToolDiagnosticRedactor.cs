using System.Collections;
using System.Text.RegularExpressions;

namespace Nodetool.SDK.Diagnostics;

/// <summary>
/// Host-neutral helpers for producing safe diagnostic values. Raw workflow
/// inputs and authentication material should never be passed to a host logger.
/// </summary>
public static partial class NodeToolDiagnosticRedactor
{
    public const string Redacted = "<redacted>";

    private static readonly string[] SensitiveNameParts =
    [
        "authorization",
        "token",
        "secret",
        "password",
        "api_key",
        "apikey",
        "cookie"
    ];

    public static Uri RedactUri(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri)
            return uri;

        var builder = new UriBuilder(uri)
        {
            UserName = "",
            Password = "",
            Query = string.IsNullOrEmpty(uri.Query) ? "" : "redacted",
            Fragment = ""
        };
        return builder.Uri;
    }

    public static IReadOnlyDictionary<string, string> RedactHeaders(
        IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);
        return headers.ToDictionary(
            pair => pair.Key,
            pair => IsSensitiveName(pair.Key)
                ? Redacted
                : string.Join(", ", pair.Value),
            StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyDictionary<string, object?> RedactWorkflowInputs(
        IReadOnlyDictionary<string, object?> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        return inputs.Keys.ToDictionary(
            key => key,
            _ => (object?)Redacted,
            StringComparer.Ordinal);
    }

    public static object? RedactValue(object? value)
    {
        if (value is null)
            return null;
        if (value is string text)
            return RedactText(text);
        if (value is IDictionary dictionary)
        {
            var result = new Dictionary<string, object?>(
                StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in dictionary)
            {
                var key = Convert.ToString(entry.Key) ?? "";
                result[key] = IsSensitiveName(key)
                    ? Redacted
                    : RedactValue(entry.Value);
            }
            return result;
        }
        if (value is IEnumerable sequence and not byte[])
            return sequence.Cast<object?>().Select(RedactValue).ToArray();
        return value;
    }

    public static string RedactText(
        string text,
        params string?[] knownSecrets)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = BearerTokenPattern().Replace(
            text,
            $"Bearer {Redacted}");
        result = AbsoluteUrlPattern().Replace(
            result,
            match => Uri.TryCreate(
                match.Value,
                UriKind.Absolute,
                out var uri)
                    ? RedactUri(uri).AbsoluteUri
                    : Redacted);
        result = DataUriPattern().Replace(
            result,
            match => $"data:{match.Groups[1].Value};base64,{Redacted}");
        foreach (var secret in knownSecrets)
        {
            if (!string.IsNullOrWhiteSpace(secret))
                result = result.Replace(
                    secret,
                    Redacted,
                    StringComparison.Ordinal);
        }
        return result;
    }

    private static bool IsSensitiveName(string name)
        => SensitiveNameParts.Any(part =>
            name.Contains(part, StringComparison.OrdinalIgnoreCase));

    [GeneratedRegex(
        @"\bBearer\s+[^\s,;]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BearerTokenPattern();

    [GeneratedRegex(
        @"data:([^;,]+);base64,[A-Za-z0-9+/=\r\n]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DataUriPattern();

    [GeneratedRegex(
        @"\b(?:https?|wss?)://[^\s""'<>]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AbsoluteUrlPattern();
}
