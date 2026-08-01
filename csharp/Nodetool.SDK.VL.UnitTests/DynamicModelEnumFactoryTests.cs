using System.Text.Json;
using Nodetool.SDK.Models;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Types;
using Nodetool.SDK.VL.Utilities;
using VL.Lib.Collections;
using Xunit;

namespace Nodetool.SDK.VL.UnitTests;

public sealed class DynamicModelEnumFactoryTests
{
    [Fact]
    public void IdenticalCatalogSnapshot_DoesNotRepublishEnumDefinitions()
    {
        try
        {
            var snapshot = Snapshot(Model("same", "Same", "openai"));

            Assert.True(DynamicModelEnumFactory.UpdateCatalog(snapshot));
            Assert.False(DynamicModelEnumFactory.UpdateCatalog(snapshot));
        }
        finally
        {
            DynamicModelEnumFactory.ResetCatalog();
        }
    }

    [Fact]
    public void CatalogEntries_UseStableTypeAndStructuredWireValues()
    {
        try
        {
            DynamicModelEnumFactory.UpdateCatalog(Snapshot(
                Model("first", "GPT Test", "openai")));
            var firstType = DynamicModelEnumFactory.GetOrCreate(
                "language_model");
            Assert.NotNull(firstType);
            Assert.True(typeof(IDynamicEnum).IsAssignableFrom(firstType));
            Assert.Same(
                firstType,
                VlTypeMapping.MapNodeInputType(
                    new NodeTypeDefinition { Type = "language_model" }).Item1);
            Assert.Same(
                firstType,
                WorkflowVlTypeMapping.GetInputTypeAndDefault(
                    new TypeMetadata { Type = "language_model" }).Type);
            var selected = Activator.CreateInstance(firstType, "GPT Test");

            Assert.True(DynamicModelEnumFactory.TryToWireValue(
                selected,
                out var wireValue));
            var wire = Assert.IsType<Dictionary<string, object?>>(wireValue);
            Assert.Equal("openai", wire["provider"]);
            Assert.Equal("first", wire["id"]);

            var restored = VlValueConversion.ConvertOrFallback(
                new Dictionary<string, object?>
                {
                    ["id"] = "first",
                    ["provider"] = "openai",
                    ["type"] = "language_model",
                    ["name"] = "Different display text"
                },
                firstType,
                null);
            Assert.Equal(
                "GPT Test",
                Assert.IsAssignableFrom<IDynamicEnum>(restored).Value);

            DynamicModelEnumFactory.UpdateCatalog(Snapshot(
                Model("second", "Claude Test", "anthropic")));
            var refreshedType = DynamicModelEnumFactory.GetOrCreate(
                "language_model");

            Assert.Same(firstType, refreshedType);
            Assert.True(DynamicModelEnumFactory.TryToWireValue(
                selected,
                out var preserved));
            Assert.Equal(
                "first",
                Assert.IsType<Dictionary<string, object?>>(preserved)["id"]);
        }
        finally
        {
            DynamicModelEnumFactory.ResetCatalog();
        }
    }

    [Fact]
    public void DuplicateNames_GetCollisionSafeLabels()
    {
        try
        {
            DynamicModelEnumFactory.UpdateCatalog(Snapshot(
                Model("same-a", "Same", "openai"),
                Model("same-b", "Same", "anthropic")));
            var type = DynamicModelEnumFactory.GetOrCreate("language_model")!;

            var first = Activator.CreateInstance(type, "Same");
            var second = Activator.CreateInstance(type, "Same (anthropic)");

            Assert.True(DynamicModelEnumFactory.TryToWireValue(first, out _));
            Assert.True(DynamicModelEnumFactory.TryToWireValue(second, out var wire));
            Assert.Equal(
                "same-b",
                Assert.IsType<Dictionary<string, object?>>(wire)["id"]);
        }
        finally
        {
            DynamicModelEnumFactory.ResetCatalog();
        }
    }

    [Theory]
    [InlineData("language_model")]
    [InlineData("image_model")]
    [InlineData("llama_model")]
    [InlineData("hf.text_generation")]
    [InlineData("tjs.image_classification")]
    public void AuthoritativeModelTypes_MapToStableDynamicEnums(
        string compatibility)
    {
        try
        {
            DynamicModelEnumFactory.UpdateCatalog(Snapshot(
                Model(
                    "model",
                    "Compatible model",
                    "local",
                    compatibility,
                    SdkModelAvailability.ReadyLocal)));

            var nodeType = VlTypeMapping.MapNodeInputType(
                new NodeTypeDefinition { Type = compatibility }).Item1;
            var workflowType = WorkflowVlTypeMapping.GetInputTypeAndDefault(
                new TypeMetadata { Type = compatibility }).Type;

            Assert.NotNull(nodeType);
            Assert.Same(nodeType, workflowType);
            Assert.True(typeof(IDynamicEnum).IsAssignableFrom(nodeType));
        }
        finally
        {
            DynamicModelEnumFactory.ResetCatalog();
        }
    }

    [Fact]
    public void MissingCatalog_UsesObjectFallbackWithoutGuessing()
    {
        DynamicModelEnumFactory.ResetCatalog();

        Assert.Equal(
            typeof(object),
            VlTypeMapping.MapNodeInputType(
                new NodeTypeDefinition { Type = "language_model" }).Item1);
        Assert.Equal(
            typeof(object),
            WorkflowVlTypeMapping.GetInputTypeAndDefault(
                new TypeMetadata { Type = "language_model" }).Type);
        Assert.Equal(
            typeof(object),
            VlTypeMapping.MapNodeInputType(
                new NodeTypeDefinition { Type = "custom_unknown_model" }).Item1);
    }

    private static ModelCatalogSnapshot Snapshot(params ModelDescriptor[] models)
        => new(
            Guid.NewGuid().ToString("N"),
            "local",
            models,
            DateTimeOffset.UtcNow,
            false,
            null);

    private static ModelDescriptor Model(
        string id,
        string name,
        string provider,
        string compatibility = "language_model",
        string availability = SdkModelAvailability.ReadyRemote)
        => new(
            $"{compatibility}|{provider}|{id}|",
            name,
            compatibility,
            availability,
            false,
            "local",
            provider,
            id,
            null,
            null,
            Array.Empty<string>(),
            null,
            Json(
                $$"""{"type":"{{compatibility}}","provider":"{{provider}}","id":"{{id}}","name":"{{name}}"}"""));

    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
