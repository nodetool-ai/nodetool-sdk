using Nodetool.SDK.Values;
using System.Text.Json;

namespace Nodetool.SDK.Tests.Values;

public class NodeToolValuePresentationTests
{
    [Fact]
    public void TypedString_IsUnwrapped()
    {
        var value = NodeToolValue.From(new Dictionary<string, object?>
        {
            ["type"] = "string",
            ["value"] = "hello"
        });

        Assert.Equal("hello", NodeToolValuePresentation.ToDisplayString(value));
    }

    [Fact]
    public void ChunkList_IsConcatenated()
    {
        var value = NodeToolValue.From(new object[]
        {
            new Dictionary<string, object?>
            {
                ["type"] = "chunk",
                ["content"] = "hel"
            },
            new Dictionary<string, object?>
            {
                ["type"] = "chunk",
                ["content"] = "lo"
            }
        });

        Assert.Equal("hello", NodeToolValuePresentation.ToDisplayString(value));
    }

    [Fact]
    public void SingletonStringList_IsUnwrapped()
    {
        var value = NodeToolValue.From(new object[] { "hello" });

        Assert.Equal("hello", NodeToolValuePresentation.ToDisplayString(value));
    }

    [Fact]
    public void MultipleStringList_RemainsStructuredJson()
    {
        var value = NodeToolValue.From(new object[] { "alpha", "beta" });

        using var document = JsonDocument.Parse(
            NodeToolValuePresentation.ToDisplayString(value));
        Assert.Equal(
            new[] { "alpha", "beta" },
            document.RootElement
                .EnumerateArray()
                .Select(item => item.GetString())
                .ToArray());
    }

    [Fact]
    public void LooseObject_PrefersTextAndPreservesUnknownMaps()
    {
        var text = NodeToolValue.From(new Dictionary<string, object?>
        {
            ["text"] = "hello",
            ["other"] = 1
        });
        var unknown = NodeToolValue.From(new Dictionary<string, object?>
        {
            ["other"] = 1
        });

        Assert.Equal("hello", NodeToolValuePresentation.ToDisplayObject(text));
        using var document = JsonDocument.Parse(
            Assert.IsType<string>(
                NodeToolValuePresentation.ToDisplayObject(unknown)));
        Assert.Equal(1, document.RootElement.GetProperty("other").GetInt32());
    }

    [Fact]
    public void EmptyPreferredField_FallsThroughToUsefulText()
    {
        var value = NodeToolValue.From(new Dictionary<string, object?>
        {
            ["uri"] = "",
            ["text"] = "hello"
        });

        Assert.Equal("hello", NodeToolValuePresentation.ToDisplayString(value));
    }
}
