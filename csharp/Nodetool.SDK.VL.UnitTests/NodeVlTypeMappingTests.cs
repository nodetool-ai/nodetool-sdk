using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Types.Assets;
using Nodetool.SDK.Values;
using Nodetool.SDK.VL.Nodes;
using Nodetool.SDK.VL.Utilities;
using VL.Lib.Collections;
using Xunit;
using VlPath = VL.Lib.IO.Path;

namespace Nodetool.SDK.VL.UnitTests;

public class NodeVlTypeMappingTests
{
    [Fact]
    public void OptionConstrainedString_UsesDynamicVlEnum()
    {
        var metadata = new NodeTypeDefinition
        {
            Type = "str",
            TypeName = "ResizeMode",
            Values = ["cover", "contain"]
        };

        var (type, _) = VlTypeMapping.MapNodeInputType(metadata);

        Assert.NotNull(type);
        Assert.True(typeof(IDynamicEnum).IsAssignableFrom(type));
    }

    [Fact]
    public void DynamicEnumType_HasCompactIdentityAndParameterlessDefault()
    {
        var (type, _) = VlTypeMapping.MapNodeType(
            new NodeTypeDefinition
            {
                Type = "enum",
                TypeName = "ResizeMode",
                Values = ["cover", "contain"]
            });
        Assert.NotNull(type);

        var instance = Activator.CreateInstance(type);

        Assert.IsAssignableFrom<IDynamicEnum>(instance);
        Assert.True(
            type.ToString().Length < 300,
            $"Dynamic enum type name is too long: {type}");
    }

    [Fact]
    public void DynamicEnumDefaultAndOutput_RestoreWireSelection()
    {
        var metadata = new NodeTypeDefinition
        {
            Type = "enum",
            TypeName = "ResizeMode",
            Values = ["cover", "contain"]
        };
        var (type, fallback) = VlTypeMapping.MapNodeType(metadata);
        Assert.NotNull(type);

        var converted = VlValueConversion.ConvertOrFallback(
            "contain",
            type,
            fallback);
        var dynamicEnum = Assert.IsAssignableFrom<IDynamicEnum>(converted);

        Assert.Equal("contain", dynamicEnum.Value);
        Assert.Equal("contain", VlValueConversion.NormalizeForTransport(converted!));
    }

    [Fact]
    public void IndividualNodeEnumOutput_RestoresWireSelection()
    {
        var metadata = new NodeTypeDefinition
        {
            Type = "enum",
            TypeName = "ResizeMode",
            Values = ["cover", "contain"]
        };
        var (type, _) = VlTypeMapping.MapNodeType(metadata);
        Assert.NotNull(type);

        var converted = NodeBase.ConvertNodeToolValueToExpectedType(
            NodeToolValue.From("contain"),
            type);

        Assert.Equal(
            "contain",
            Assert.IsAssignableFrom<IDynamicEnum>(converted).Value);
    }

    [Fact]
    public void InvalidByteList_DoesNotWrapValues()
    {
        var converted = NodeBase.ConvertNodeToolValueToExpectedType(
            NodeToolValue.From(new[] { 1, 256 }),
            typeof(byte[]));

        Assert.Empty(Assert.IsType<byte[]>(converted));
    }

    [Fact]
    public void DynamicEnumSpread_NormalizesEachWireValue()
    {
        var metadata = new NodeTypeDefinition
        {
            Type = "enum",
            TypeName = "Quality",
            Values = [1L, 2L]
        };
        var (elementType, fallback) = VlTypeMapping.MapNodeType(metadata);
        Assert.NotNull(elementType);
        var selected = VlValueConversion.ConvertOrFallback(
            2L,
            elementType,
            fallback);
        var spread = VlValueConversion.CreateCollection(
            typeof(Spread<>).MakeGenericType(elementType!),
            elementType!,
            [selected]);

        var normalized = Assert.IsType<object[]>(
            VlValueConversion.NormalizeDynamicEnumsForTransport(spread));

        Assert.Equal(2L, Assert.Single(normalized));
    }

    [Fact]
    public void ListElementType_IsMappedRecursively()
    {
        var metadata = new NodeTypeDefinition
        {
            Type = "list",
            TypeArgs = [new NodeTypeDefinition { Type = "int" }]
        };

        var (type, defaultValue) = VlTypeMapping.MapNodeType(metadata);

        Assert.Equal(typeof(Spread<int>), type);
        Assert.Empty(Assert.IsType<Spread<int>>(defaultValue));
    }

    [Fact]
    public void BinaryType_RemainsBinary()
    {
        var (type, defaultValue) = VlTypeMapping.MapNodeType(
            new NodeTypeDefinition { Type = "bytes" });

        Assert.Equal(typeof(byte[]), type);
        Assert.Empty(Assert.IsType<byte[]>(defaultValue));
    }

    [Fact]
    public void UnknownStructuredType_UsesObjectFallback()
    {
        var (type, defaultValue) = VlTypeMapping.MapNodeType(
            new NodeTypeDefinition { Type = "dataframe" });

        Assert.Equal(typeof(object), type);
        Assert.Null(defaultValue);
    }

    [Theory]
    [InlineData("image", typeof(ImageRef))]
    [InlineData("audio", typeof(AudioRef))]
    [InlineData("video", typeof(VideoRef))]
    [InlineData("document", typeof(DocumentRef))]
    [InlineData("asset", typeof(GenericAssetRef))]
    [InlineData("folder", typeof(FolderRef))]
    [InlineData("model_ref", typeof(ModelRef))]
    [InlineData("model_3d", typeof(Model3DRef))]
    [InlineData("font", typeof(FontRef))]
    public void AssetReferenceTypes_UseTypedPins(string typeName, Type expectedType)
    {
        var (type, defaultValue) = VlTypeMapping.MapNodeType(
            new NodeTypeDefinition { Type = typeName });

        Assert.Equal(expectedType, type);
        Assert.IsType(expectedType, defaultValue);
    }

    [Theory]
    [InlineData("audio")]
    [InlineData("video")]
    [InlineData("document")]
    [InlineData("asset")]
    [InlineData("folder")]
    [InlineData("model_ref")]
    [InlineData("model_3d")]
    [InlineData("font")]
    public void FileBackedAssetInputs_UseNativeVlPath(string typeName)
    {
        var (type, defaultValue) = VlTypeMapping.MapNodeInputType(
            new NodeTypeDefinition { Type = typeName });

        Assert.Equal(typeof(VlPath), type);
        Assert.IsType<VlPath>(defaultValue);
    }

    [Fact]
    public void ImageInput_RemainsATypedImageReference()
    {
        var (type, defaultValue) = VlTypeMapping.MapNodeInputType(
            new NodeTypeDefinition { Type = "image" });

        Assert.Equal(typeof(ImageRef), type);
        Assert.IsType<ImageRef>(defaultValue);
    }

    [Fact]
    public void FileBackedAssetInputList_UsesNativeVlPathSpread()
    {
        var (type, defaultValue) = VlTypeMapping.MapNodeInputType(
            new NodeTypeDefinition
            {
                Type = "list",
                TypeArgs = [new NodeTypeDefinition { Type = "audio" }]
            });

        Assert.Equal(typeof(Spread<VlPath>), type);
        Assert.Empty(Assert.IsType<Spread<VlPath>>(defaultValue));
    }

    [Fact]
    public void AnyWithAudioRefTypeName_UsesTypedAudioPin()
    {
        var (type, defaultValue) = VlTypeMapping.MapNodeType(
            new NodeTypeDefinition
            {
                Type = "any",
                TypeName = "AudioRef"
            });

        Assert.Equal(typeof(AudioRef), type);
        Assert.IsType<AudioRef>(defaultValue);
    }

    [Fact]
    public void NamespacedAssetTypeName_UsesTypedPin()
    {
        var (type, defaultValue) = VlTypeMapping.MapNodeType(
            new NodeTypeDefinition
            {
                Type = "any",
                TypeName = "nodetool.types.AudioRef"
            });

        Assert.Equal(typeof(AudioRef), type);
        Assert.IsType<AudioRef>(defaultValue);
    }

    [Fact]
    public void ListTypeName_DoesNotOverrideCollectionShape()
    {
        var (type, defaultValue) = VlTypeMapping.MapNodeType(
            new NodeTypeDefinition
            {
                Type = "list",
                TypeName = "VideoRef",
                TypeArgs = [new NodeTypeDefinition { Type = "video" }]
            });

        Assert.Equal(typeof(Spread<VideoRef>), type);
        Assert.Empty(Assert.IsType<Spread<VideoRef>>(defaultValue));
    }

    [Fact]
    public void AssetReferenceList_UsesTypedVlSpread()
    {
        var (type, defaultValue) = VlTypeMapping.MapNodeType(
            new NodeTypeDefinition
            {
                Type = "list",
                TypeArgs = [new NodeTypeDefinition { Type = "video" }]
            });

        Assert.Equal(typeof(Spread<VideoRef>), type);
        Assert.Empty(Assert.IsType<Spread<VideoRef>>(defaultValue));
    }
}
