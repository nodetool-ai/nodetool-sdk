# NodeTool C# Type System Enhancement Plan

## 🎯 **Current Status**

✅ **Core Type Infrastructure** - `BaseType`, `TypeMetadata`, type registry  
✅ **Basic Asset Types** - `ImageRef`, `AudioRef`, `VideoRef`, etc.  
✅ **Type Mapping Utilities** - `TypeMapper` for metadata to C# types  
✅ **VL Type Integration** - Basic mapping in factories  
❌ **Advanced VL Types** - SKImage, byte[] for multimedia  
❌ **Asset Conversion Pipeline** - Upload/download with caching  
❌ **Union Types** - Proper handling of `Union[str, int]` etc.

## 📋 **Enhancement Plan: Production-Ready Type System**

NOTE: a first step is to get the request and response of running a workflow and then
adapt the plan according to the findings.

### **Phase 1: Enhanced VL Type Mapping** _(2-3 days)_

**🎯 Goal**: Map NodeTool types to optimal VL native types

#### **1.1 Enhanced Type Mapping Strategy**

| **NodeTool Type** | **Current VL Type** | **Enhanced VL Type**           | **Benefits**              |
| ----------------- | ------------------- | ------------------------------ | ------------------------- |
| `image`           | `string`            | `SKImage`                      | Native image manipulation |
| `audio`           | `string`            | `byte[]`                       | Direct audio processing   |
| `video`           | `string`            | `byte[]`                       | Video data handling       |
| `tensor`          | `string`            | `float[]`                      | Numeric computations      |
| `dataframe`       | `string`            | `Dictionary<string, object[]>` | Structured data           |
| `union`           | `string`            | `object`                       | Type flexibility          |

#### **1.2 Update VL Type Mapper**

**File**: `nodetool-sdk/csharp/Nodetool.SDK.VL/TypeSystem/VLTypeMapper.cs`

```csharp
public static class VLTypeMapper
{
    private static readonly Dictionary<string, Type> EnhancedTypeMap = new()
    {
        // Multimedia types
        ["image"] = typeof(SKImage),
        ["audio"] = typeof(byte[]),
        ["video"] = typeof(byte[]),

        // Data types
        ["tensor"] = typeof(float[]),
        ["dataframe"] = typeof(Dictionary<string, object[]>),
        ["nparray"] = typeof(float[]),

        // Collections
        ["list"] = typeof(object[]),
        ["dict"] = typeof(Dictionary<string, object>),

        // Primitives
        ["str"] = typeof(string),
        ["int"] = typeof(int),
        ["float"] = typeof(double),
        ["bool"] = typeof(bool),

        // Special
        ["union"] = typeof(object),
        ["any"] = typeof(object),
    };

    public static (Type VLType, object? DefaultValue) MapToVLType(TypeMetadata typeMetadata)
    {
        var baseType = MapBaseType(typeMetadata);
        var defaultValue = GetDefaultValueForVLType(baseType);

        // Handle optional types
        if (typeMetadata.Optional && baseType.IsValueType)
        {
            baseType = typeof(Nullable<>).MakeGenericType(baseType);
            defaultValue = null;
        }

        return (baseType, defaultValue);
    }

    private static Type MapBaseType(TypeMetadata typeMetadata)
    {
        // Handle generic types (List<T>, Dict<K,V>)
        if (typeMetadata.TypeArgs?.Any() == true)
        {
            return MapGenericType(typeMetadata);
        }

        // Handle union types
        if (typeMetadata.Type == "union")
        {
            return MapUnionType(typeMetadata);
        }

        // Handle enum types
        if (typeMetadata.Values?.Any() == true)
        {
            return typeof(string); // Enums as strings for VL compatibility
        }

        // Handle basic types
        return EnhancedTypeMap.GetValueOrDefault(typeMetadata.Type, typeof(string));
    }
}
```

#### **1.3 Generic Type Handling**

```csharp
private static Type MapGenericType(TypeMetadata typeMetadata)
{
    return typeMetadata.Type switch
    {
        "list" => MapListType(typeMetadata),
        "dict" => MapDictionaryType(typeMetadata),
        "tuple" => MapTupleType(typeMetadata),
        _ => typeof(object)
    };
}

private static Type MapListType(TypeMetadata typeMetadata)
{
    if (typeMetadata.TypeArgs?.FirstOrDefault() is var elementType && elementType != null)
    {
        var vlElementType = MapBaseType(elementType);
        return vlElementType.MakeArrayType(); // T[] instead of List<T> for VL
    }

    return typeof(object[]);
}

private static Type MapDictionaryType(TypeMetadata typeMetadata)
{
    // VL prefers Dictionary<string, object> for flexibility
    return typeof(Dictionary<string, object>);
}
```

### **Phase 2: Asset Conversion Pipeline** _(3-4 days)_

**🎯 Goal**: Seamless conversion between NodeTool assets and VL types

#### **2.1 Asset Converter Service**

**File**: `nodetool-sdk/csharp/Nodetool.SDK/Services/AssetConverter.cs`

```csharp
public class AssetConverter : IDisposable
{
    private readonly INodetoolClient _client;
    private readonly MemoryCache _cache;
    private readonly ILogger _logger;

    private const int MaxCacheSize = 100;
    private const long MaxCacheMemoryMB = 500;

    public AssetConverter(INodetoolClient client, ILogger? logger = null)
    {
        _client = client;
        _logger = logger ?? new NullLogger();
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = MaxCacheSize
        });
    }

    #region SKImage Conversions

    public async Task<SKImage?> DownloadImageAsync(string uri)
    {
        var cacheKey = $"image:{uri}";

        if (_cache.TryGetValue(cacheKey, out SKImage? cached))
        {
            _logger.LogDebug($"Image cache hit: {uri}");
            return cached;
        }

        try
        {
            _logger.LogDebug($"Downloading image: {uri}");

            byte[] imageData = await DownloadBytesFromUri(uri);
            var skImage = SKImage.FromEncodedData(imageData);

            if (skImage != null)
            {
                _cache.Set(cacheKey, skImage, TimeSpan.FromMinutes(30));
                _logger.LogDebug($"Image cached: {uri} ({imageData.Length} bytes)");
            }

            return skImage;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to download image {uri}: {ex.Message}");
            return null;
        }
    }

    public async Task<string> UploadImageAsync(SKImage image)
    {
        try
        {
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 90);
            using var stream = new MemoryStream(encoded.ToArray());

            var asset = await _client.UploadAssetAsync($"image_{Guid.NewGuid()}.png", stream);
            _logger.LogDebug($"Image uploaded: {asset.Id} ({encoded.Size} bytes)");

            return asset.Uri;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to upload image: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Audio/Video Conversions

    public async Task<byte[]?> DownloadBytesAsync(string uri)
    {
        var cacheKey = $"bytes:{uri}";

        if (_cache.TryGetValue(cacheKey, out byte[]? cached))
        {
            _logger.LogDebug($"Bytes cache hit: {uri}");
            return cached;
        }

        try
        {
            _logger.LogDebug($"Downloading bytes: {uri}");

            byte[] data = await DownloadBytesFromUri(uri);
            _cache.Set(cacheKey, data, TimeSpan.FromMinutes(15));

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to download bytes {uri}: {ex.Message}");
            return null;
        }
    }

    public async Task<string> UploadBytesAsync(byte[] data, string contentType = "application/octet-stream")
    {
        try
        {
            var extension = GetExtensionForContentType(contentType);
            using var stream = new MemoryStream(data);

            var asset = await _client.UploadAssetAsync($"asset_{Guid.NewGuid()}{extension}", stream);
            _logger.LogDebug($"Bytes uploaded: {asset.Id} ({data.Length} bytes)");

            return asset.Uri;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to upload bytes: {ex.Message}");
            throw;
        }
    }

    #endregion
}
```

#### **2.2 Data URI Support**

```csharp
public static class DataUriConverter
{
    public static SKImage? FromDataUri(string dataUri)
    {
        if (!dataUri.StartsWith("data:image/"))
            return null;

        try
        {
            var base64 = dataUri.Substring(dataUri.IndexOf(",") + 1);
            var bytes = Convert.FromBase64String(base64);
            return SKImage.FromEncodedData(bytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse data URI: {ex.Message}");
            return null;
        }
    }

    public static string ToDataUri(SKImage image, SKEncodedImageFormat format = SKEncodedImageFormat.Png)
    {
        using var encoded = image.Encode(format, 90);
        var base64 = Convert.ToBase64String(encoded.ToArray());
        var mimeType = GetMimeType(format);
        return $"data:{mimeType};base64,{base64}";
    }

    private static string GetMimeType(SKEncodedImageFormat format)
    {
        return format switch
        {
            SKEncodedImageFormat.Png => "image/png",
            SKEncodedImageFormat.Jpeg => "image/jpeg",
            SKEncodedImageFormat.Webp => "image/webp",
            _ => "image/png"
        };
    }
}
```

### **Phase 3: Union Type Support** _(2-3 days)_

**🎯 Goal**: Handle NodeTool's union types properly in VL

#### **3.1 Union Type Implementation**

**File**: `nodetool-sdk/csharp/Nodetool.SDK/Types/UnionType.cs`

```csharp
public class UnionType<T1, T2>
{
    private readonly object _value;
    private readonly Type _actualType;

    public UnionType(T1 value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
        _actualType = typeof(T1);
    }

    public UnionType(T2 value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
        _actualType = typeof(T2);
    }

    public bool Is<T>() => _actualType == typeof(T);

    public T As<T>()
    {
        if (!Is<T>())
            throw new InvalidCastException($"Union contains {_actualType.Name}, not {typeof(T).Name}");
        return (T)_value;
    }

    public T? TryAs<T>() where T : class
    {
        return Is<T>() ? (T)_value : null;
    }

    public override string ToString() => _value?.ToString() ?? "";

    public static implicit operator UnionType<T1, T2>(T1 value) => new(value);
    public static implicit operator UnionType<T1, T2>(T2 value) => new(value);
}

// For VL compatibility, also provide object-based union
public class FlexibleUnion
{
    public object? Value { get; }
    public Type ActualType { get; }

    public FlexibleUnion(object? value)
    {
        Value = value;
        ActualType = value?.GetType() ?? typeof(object);
    }

    public T? As<T>() => Value is T t ? t : default;
    public override string ToString() => Value?.ToString() ?? "";
}
```

#### **3.2 Union Type Conversion in VL**

```csharp
public static class UnionTypeConverter
{
    public static object ConvertUnionForVL(TypeMetadata unionType, object? value)
    {
        if (unionType.Type != "union" || unionType.TypeArgs == null)
            return value ?? "";

        // Check if value matches any of the union type arguments
        foreach (var typeArg in unionType.TypeArgs)
        {
            if (IsValueOfType(value, typeArg))
            {
                return ConvertToVLType(value, typeArg);
            }
        }

        // Fallback: convert to string
        return value?.ToString() ?? "";
    }

    private static bool IsValueOfType(object? value, TypeMetadata typeMetadata)
    {
        if (value == null) return typeMetadata.Optional;

        return typeMetadata.Type switch
        {
            "str" => value is string,
            "int" => value is int or long,
            "float" => value is float or double,
            "bool" => value is bool,
            "image" => value is SKImage or string,
            _ => true // Allow anything for complex types
        };
    }
}
```

### **Phase 4: Enhanced JSON Serialization** _(1-2 days)_

**🎯 Goal**: Proper JSON handling for complex NodeTool types

#### **4.1 Custom JSON Converters**

**File**: `nodetool-sdk/csharp/Nodetool.SDK/Serialization/NodetoolJsonConverter.cs`

```csharp
public class NodetoolJsonConverter : JsonConverter<BaseType>
{
    public override BaseType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProperty))
            throw new JsonException("Missing 'type' property in BaseType JSON");

        var typeName = typeProperty.GetString();
        var targetType = BaseType.GetType(typeName);

        if (targetType == null)
            throw new JsonException($"Unknown type: {typeName}");

        return (BaseType?)JsonSerializer.Deserialize(root.GetRawText(), targetType, options);
    }

    public override void Write(Utf8JsonWriter writer, BaseType value, JsonSerializerOptions options)
    {
        var dict = value.ToDict();
        JsonSerializer.Serialize(writer, dict, options);
    }
}

public class TypeMetadataJsonConverter : JsonConverter<TypeMetadata>
{
    public override TypeMetadata? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        return new TypeMetadata
        {
            Type = root.GetProperty("type").GetString() ?? "",
            Optional = root.TryGetProperty("optional", out var opt) && opt.GetBoolean(),
            Values = ParseValues(root),
            TypeArgs = ParseTypeArgs(root, options),
            TypeName = root.TryGetProperty("type_name", out var tn) ? tn.GetString() : null
        };
    }

    // ... implementation details
}
```

### **Phase 5: Performance & Memory Management** _(2-3 days)_

**🎯 Goal**: Efficient memory usage and asset lifecycle management

#### **5.1 Asset Cache Management**

```csharp
public class AssetCacheManager : IDisposable
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly Timer _cleanupTimer;
    private readonly SemaphoreSlim _cleanupSemaphore = new(1, 1);

    public AssetCacheManager()
    {
        _cleanupTimer = new Timer(CleanupExpiredEntries, null,
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiry = null)
        where T : class, IDisposable
    {
        if (_cache.TryGetValue(key, out var existing) &&
            existing.Value is T existingValue &&
            !existing.IsExpired)
        {
            existing.UpdateLastAccess();
            return existingValue;
        }

        var newValue = await factory();
        if (newValue != null)
        {
            var entry = new CacheEntry(newValue, expiry ?? TimeSpan.FromMinutes(30));
            _cache.AddOrUpdate(key, entry, (k, old) => {
                old.Dispose();
                return entry;
            });
        }

        return newValue;
    }

    private void CleanupExpiredEntries(object? state)
    {
        if (!_cleanupSemaphore.Wait(100)) return;

        try
        {
            var expiredKeys = _cache
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                if (_cache.TryRemove(key, out var entry))
                {
                    entry.Dispose();
                }
            }

            // Also clean up LRU if cache is too large
            if (_cache.Count > 100)
            {
                var lruKeys = _cache
                    .OrderBy(kvp => kvp.Value.LastAccess)
                    .Take(_cache.Count - 80)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in lruKeys)
                {
                    if (_cache.TryRemove(key, out var entry))
                    {
                        entry.Dispose();
                    }
                }
            }
        }
        finally
        {
            _cleanupSemaphore.Release();
        }
    }
}
```

### **Phase 6: Testing & Validation** _(2-3 days)_

#### **6.1 Type Mapping Tests**

```csharp
[TestFixture]
public class VLTypeMappingTests
{
    [Test]
    public void TestImageTypeMapping()
    {
        var imageType = new TypeMetadata { Type = "image" };
        var (vlType, defaultValue) = VLTypeMapper.MapToVLType(imageType);

        Assert.AreEqual(typeof(SKImage), vlType);
        Assert.IsNull(defaultValue);
    }

    [Test]
    public void TestUnionTypeMapping()
    {
        var unionType = new TypeMetadata
        {
            Type = "union",
            TypeArgs = new List<TypeMetadata>
            {
                new() { Type = "str" },
                new() { Type = "int" }
            }
        };

        var (vlType, defaultValue) = VLTypeMapper.MapToVLType(unionType);
        Assert.AreEqual(typeof(object), vlType);
    }
}
```

#### **6.2 Asset Conversion Tests**

```csharp
[TestFixture]
public class AssetConversionTests
{
    private AssetConverter _converter;
    private Mock<INodetoolClient> _mockClient;

    [SetUp]
    public void Setup()
    {
        _mockClient = new Mock<INodetoolClient>();
        _converter = new AssetConverter(_mockClient.Object);
    }

    [Test]
    public async Task TestImageUploadDownload()
    {
        // Create test image
        var testImage = CreateTestSKImage();

        // Mock upload response
        _mockClient.Setup(c => c.UploadAssetAsync(It.IsAny<string>(), It.IsAny<Stream>()))
            .ReturnsAsync(new AssetResponse { Id = "test-id", Uri = "http://test.com/image.png" });

        // Test upload
        var uri = await _converter.UploadImageAsync(testImage);
        Assert.IsNotEmpty(uri);

        // Mock download response
        var imageBytes = GetTestImageBytes();
        _mockClient.Setup(c => c.DownloadAssetAsync("test-id"))
            .ReturnsAsync(new MemoryStream(imageBytes));

        // Test download
        var downloadedImage = await _converter.DownloadImageAsync(uri);
        Assert.IsNotNull(downloadedImage);
    }
}
```

## 📊 **Success Metrics**

### **Type System**:

- ✅ All NodeTool types map to appropriate VL types
- ✅ Union types handle gracefully without crashes
- ✅ Generic types (List<T>, Dict<K,V>) work correctly
- ✅ Asset types integrate seamlessly with VL multimedia

### **Performance**:

- ✅ Asset caching reduces redundant operations by 80%+
- ✅ Memory usage stays under 500MB during normal use
- ✅ Type conversions complete in < 100ms
- ✅ Cache cleanup prevents memory leaks

### **Reliability**:

- ✅ Robust error handling for type conversion failures
- ✅ Graceful degradation for unsupported types
- ✅ No memory leaks from cached assets
- ✅ Thread-safe operations throughout

## ⏱️ **Timeline**

- **Week 1**: Enhanced VL type mapping + union type support
- **Week 2**: Asset conversion pipeline + JSON serialization
- **Week 3**: Performance optimization + testing

**Total Duration**: ~3 weeks for production-ready type system

## 🔗 **Integration Points**

- **Supports**: Both node execution and workflow execution plans
- **Provides**: Asset conversion services for multimedia nodes
- **Enables**: Proper VL integration with native multimedia types
- **Requires**: SkiaSharp dependency for image handling

This enhanced type system will provide the foundation for seamless NodeTool integration with VL's native types and multimedia capabilities.
