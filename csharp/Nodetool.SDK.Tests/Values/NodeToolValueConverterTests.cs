using System.Text.Json;
using Nodetool.SDK.Values;
using Nodetool.SDK.Types.Assets;

namespace Nodetool.SDK.Tests.Values;

public class NodeToolValueConverterTests
{
    [Fact]
    public void JsonMap_ConvertsToHostNeutralClrTree()
    {
        using var document = JsonDocument.Parse(
            """{"name":"test","values":[1,true]}""");

        var converted = NodeToolValueConverter.Convert(
            document.RootElement,
            typeof(object));

        var map = Assert.IsType<Dictionary<string, object?>>(converted);
        Assert.Equal("test", map["name"]);
        Assert.Equal(
            new object?[] { 1, true },
            Assert.IsType<object?[]>(map["values"]));
    }

    [Fact]
    public void NodeToolList_ConvertsRecursivelyToTypedArray()
    {
        var value = NodeToolValue.From(new object[] { "1", 2L, 3.0 });

        var converted = NodeToolValueConverter.Convert(value, typeof(int[]));

        Assert.Equal(new[] { 1, 2, 3 }, Assert.IsType<int[]>(converted));
    }

    [Fact]
    public void ReadOnlyListTarget_ReturnsAssignableTypedCollection()
    {
        var converted = NodeToolValueConverter.Convert(
            new object[] { 1L, 2L },
            typeof(IReadOnlyList<int>));

        Assert.Equal(
            new[] { 1, 2 },
            Assert.IsAssignableFrom<IReadOnlyList<int>>(converted));
    }

    [Fact]
    public void FractionalInteger_ReturnsStableError()
    {
        var success = NodeToolValueConverter.TryConvert(
            1.5,
            typeof(int),
            out _,
            out var error);

        Assert.False(success);
        Assert.Equal("fractional_integer", error?.Code);
    }

    [Fact]
    public void IntegerOverflow_ReturnsStableError()
    {
        var success = NodeToolValueConverter.TryConvert(
            (long)int.MaxValue + 1,
            typeof(int),
            out _,
            out var error);

        Assert.False(success);
        Assert.Equal("numeric_overflow", error?.Code);
    }

    [Fact]
    public void CollectionItemError_ContainsIndexAndUnderlyingCode()
    {
        var success = NodeToolValueConverter.TryConvert(
            new object[] { 1, 2.5 },
            typeof(int[]),
            out _,
            out var error);

        Assert.False(success);
        Assert.Equal("collection_item_fractional_integer", error?.Code);
        Assert.Contains("item 1", error?.Message);
    }

    [Fact]
    public void StructuredMap_ConvertsToClrRecord()
    {
        var converted = NodeToolValueConverter.Convert(
            new Dictionary<string, object?>
            {
                ["name"] = "alpha",
                ["count"] = 3L
            },
            typeof(TestRecord));

        var record = Assert.IsType<TestRecord>(converted);
        Assert.Equal("alpha", record.Name);
        Assert.Equal(3, record.Count);
    }

    [Fact]
    public void NormalizeForTransport_RecursesThroughCollectionsAndAssets()
    {
        var value = new object[]
        {
            new AudioRef { Uri = "asset://audio-1", AssetId = "audio-1" },
            new Dictionary<string, object?>
            {
                ["nested"] = new[] { 1, 2 }
            }
        };

        var normalized = Assert.IsType<object?[]>(
            NodeToolValueConverter.NormalizeForTransport(value));

        var asset = Assert.IsType<Dictionary<string, object?>>(normalized[0]);
        Assert.Equal("audio-1", asset["asset_id"]);
        var map = Assert.IsType<Dictionary<string, object?>>(normalized[1]);
        Assert.Equal(
            new object?[] { 1, 2 },
            Assert.IsType<object?[]>(map["nested"]));
    }

    private sealed class TestRecord
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }
}
