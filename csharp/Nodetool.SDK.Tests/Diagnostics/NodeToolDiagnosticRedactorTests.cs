using Nodetool.SDK.Diagnostics;

namespace Nodetool.SDK.Tests.Diagnostics;

public sealed class NodeToolDiagnosticRedactorTests
{
    [Fact]
    public void RedactUri_RemovesCredentialsQueryAndFragment()
    {
        var result = NodeToolDiagnosticRedactor.RedactUri(
            new Uri(
                "https://user:pass@example.test/api?token=secret#fragment"));

        Assert.Equal(
            "https://example.test/api?redacted",
            result.AbsoluteUri);
    }

    [Fact]
    public void RedactValue_RemovesNestedSecretsAndInlineMedia()
    {
        var value = new Dictionary<string, object?>
        {
            ["name"] = "visible",
            ["api_key"] = "secret",
            ["nested"] = new Dictionary<string, object?>
            {
                ["authorization"] = "Bearer abc",
                ["image"] = "data:image/png;base64,AAAA"
            }
        };

        var result = Assert.IsType<Dictionary<string, object?>>(
            NodeToolDiagnosticRedactor.RedactValue(value));
        Assert.Equal("visible", result["name"]);
        Assert.Equal(NodeToolDiagnosticRedactor.Redacted, result["api_key"]);
        var nested = Assert.IsType<Dictionary<string, object?>>(
            result["nested"]);
        Assert.Equal(
            NodeToolDiagnosticRedactor.Redacted,
            nested["authorization"]);
        Assert.Equal(
            "data:image/png;base64,<redacted>",
            nested["image"]);
    }

    [Fact]
    public void RedactWorkflowInputs_PreservesOnlyPinNames()
    {
        var result = NodeToolDiagnosticRedactor.RedactWorkflowInputs(
            new Dictionary<string, object?>
            {
                ["prompt"] = "private prompt",
                ["image"] = new byte[] { 1, 2, 3 }
            });

        Assert.Equal(["prompt", "image"], result.Keys);
        Assert.All(
            result.Values,
            value => Assert.Equal(
                NodeToolDiagnosticRedactor.Redacted,
                value));
    }

    [Fact]
    public void RedactText_RemovesBearerKnownSecretAndDataUri()
    {
        var result = NodeToolDiagnosticRedactor.RedactText(
            "Bearer abc token=xyz data:text/plain;base64,SGVsbG8= " +
            "https://user:pass@example.test/run?token=abc",
            "xyz");

        Assert.DoesNotContain("abc", result);
        Assert.DoesNotContain("xyz", result);
        Assert.DoesNotContain("SGVsbG8", result);
        Assert.DoesNotContain("user", result);
        Assert.DoesNotContain("pass", result);
        Assert.DoesNotContain("token=abc", result);
        Assert.Contains("https://example.test/run?redacted", result);
    }
}
