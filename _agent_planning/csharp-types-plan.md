# NodeTool C# Type System Enhancement Plan

## 🎯 **Current Status**

✅ **Core Type Infrastructure** - `BaseType`, `TypeMetadata`, type registry  
✅ **Basic Asset Types** - `ImageRef`, `AudioRef`, `VideoRef`, etc.  
✅ **Type Mapping Utilities** - `TypeMapper` for metadata to C# types  
✅ **VL Type Integration** - Basic mapping in factories  
❌ **SDK Data Object Interface** - Clean abstraction over WebSocket data  
❌ **VL Type Conversion Service** - Simple transformation layer

## 🔍 **Real Data Object Format** _(From WebSocket Execution)_

**Key Discovery**: NodeTool WebSocket messages contain `NodeToolDataObject` format:

```json
{
  "type": "image",
  "uri": "http://...",
  "asset_id": "...",
  "data": {
    /* actual embedded data */
  },
  "width": 1024,
  "height": 768,
  "format": "png"
}
```

### **Data Object Characteristics**:

- **Embedded Data**: `data` field contains actual content (base64, structured data)
- **Asset References**: `uri` and `asset_id` for downloadable assets
- **Type Metadata**: `width`, `height`, `duration`, `format`, etc.
- **Dual Nature**: Objects can have BOTH embedded data AND asset references

## 📋 **Implementation Plan: Simplified Type Handling** ⭐ **UPDATED**

### **Phase 1: SDK Data Object Interface** _(1-2 days)_

**🎯 Goal**: SDK provides clean interface, VL just does type conversion

#### **1.1 SDK Handles All Data Complexity**

**File**: `nodetool-sdk/csharp/Nodetool.SDK/Services/IExecutionSession.cs`

```csharp
// SDK provides simple output access
public interface IExecutionSession
{
    // Raw data objects
    T? GetOutput<T>(string outputName);
    NodeToolDataObject? GetOutputData(string outputName);

    // Common conversions built into SDK
    string? GetOutputAsString(string outputName);
    byte[]? GetOutputAsBytes(string outputName);

    // All outputs
    Dictionary<string, object> GetAllOutputs();
}

// SDK internal implementation handles all WebSocket complexity
internal class ExecutionSession : IExecutionSession
{
    private readonly ConcurrentDictionary<string, NodeToolDataObject> _outputs = new();

    public T? GetOutput<T>(string outputName)
    {
        if (_outputs.TryGetValue(outputName, out var dataObject))
        {
            return ConvertDataObject<T>(dataObject);
        }
        return default;
    }

    public NodeToolDataObject? GetOutputData(string outputName)
    {
        return _outputs.GetValueOrDefault(outputName);
    }

    private T? ConvertDataObject<T>(NodeToolDataObject dataObject)
    {
        // SDK handles basic conversions that work across all platforms
        if (typeof(T) == typeof(NodeToolDataObject))
            return (T)(object)dataObject;

        if (typeof(T) == typeof(string))
            return (T)(object)dataObject.GetEmbeddedData<string>();

        if (typeof(T) == typeof(byte[]))
        {
            var base64 = dataObject.GetEmbeddedData<string>();
            if (base64 != null)
                return (T)(object)Convert.FromBase64String(base64);
        }

        return (T)(object)dataObject; // Return raw for platform-specific conversion
    }
}
```

### **Phase 2: Simple VL Type Conversion** _(1-2 days)_

**🎯 Goal**: VL focuses only on converting SDK data to VL-specific types

#### **2.1 VL Type Converter Service** _(Simplified)_

**File**: `nodetool-sdk/csharp/Nodetool.SDK.VL/Services/VLTypeConverter.cs`

```csharp
public class VLTypeConverter
{
    private readonly AssetDownloadService _assetService;

    public VLTypeConverter(AssetDownloadService assetService)
    {
        _assetService = assetService;
    }

    // Single method - convert SDK data object to VL type
    public object? ConvertToVLType(NodeToolDataObject dataObject, Type targetVLType)
    {
        if (dataObject == null) return GetDefaultValue(targetVLType);

        return targetVLType switch
        {
            var t when t == typeof(SKImage) && dataObject.IsImage => ConvertToSKImage(dataObject),
            var t when t == typeof(byte[]) => ConvertToByteArray(dataObject),
            var t when t == typeof(string) => ConvertToString(dataObject),
            var t when t == typeof(float) => ConvertToFloat(dataObject),
            var t when t == typeof(double) => ConvertToDouble(dataObject),
            var t when t == typeof(int) => ConvertToInt(dataObject),
            var t when t == typeof(bool) => ConvertToBool(dataObject),
            _ => dataObject // Fallback to raw data object
        };
    }

    private SKImage? ConvertToSKImage(NodeToolDataObject dataObject)
    {
        // Priority 1: Embedded data (synchronous)
        if (dataObject.HasEmbeddedData)
        {
            var base64 = dataObject.GetEmbeddedData<string>();
            if (!string.IsNullOrEmpty(base64))
            {
                try
                {
                    var bytes = Convert.FromBase64String(base64);
                    return SKImage.FromEncodedData(bytes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to decode embedded image: {ex.Message}");
                }
            }
        }

        // Priority 2: Asset reference (could be async, for now return null)
        // TODO: Consider async asset loading strategy
        if (dataObject.HasAssetReference)
        {
            // For now, VL nodes will need to handle asset loading separately
            // or we implement a caching strategy
        }

        return null;
    }

    private byte[]? ConvertToByteArray(NodeToolDataObject dataObject)
    {
        if (dataObject.HasEmbeddedData)
        {
            var base64 = dataObject.GetEmbeddedData<string>();
            if (!string.IsNullOrEmpty(base64))
            {
                try
                {
                    return Convert.FromBase64String(base64);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to decode embedded bytes: {ex.Message}");
                }
            }
        }

        return null;
    }

    private string? ConvertToString(NodeToolDataObject dataObject)
    {
        // For text data, extract embedded content
        if (dataObject.IsText && dataObject.HasEmbeddedData)
        {
            return dataObject.GetEmbeddedData<string>();
        }

        // Fallback: JSON representation
        return JsonSerializer.Serialize(dataObject, new JsonSerializerOptions { WriteIndented = true });
    }

    private float ConvertToFloat(NodeToolDataObject dataObject)
    {
        if (dataObject.HasEmbeddedData)
        {
            var value = dataObject.GetEmbeddedData<object>();
            if (value is float f) return f;
            if (value is double d) return (float)d;
            if (float.TryParse(value?.ToString(), out var parsed)) return parsed;
        }
        return 0f;
    }

    private static object? GetDefaultValue(Type type)
    {
        if (type == typeof(SKImage)) return null;
        if (type == typeof(byte[])) return Array.Empty<byte>();
        if (type == typeof(string)) return "";
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
```

#### **2.2 Ultra-Simple VL Node Integration**

**File**: `nodetool-sdk/csharp/Nodetool.SDK.VL/Nodes/WorkflowNodeBase.cs` _(Simplified Update)_

```csharp
private void UpdateOutputPins()
{
    if (_currentSession == null) return;

    // Status pins (simple)
    _outputPins["IsRunning"].Value = _currentSession.IsRunning;
    _outputPins["Error"].Value = _currentSession.ErrorMessage ?? "";
    _outputPins["Progress"].Value = _currentSession.ProgressPercent;

    // Data pins - VL's only job: type conversion
    foreach (var outputPin in _outputPins.Where(p => IsDataPin(p.Key)))
    {
        // Get raw data from SDK
        var dataObject = _currentSession.GetOutputData(outputPin.Key);

        if (dataObject != null)
        {
            // Convert to VL type - this is VL's only responsibility
            outputPin.Value = _typeConverter.ConvertToVLType(dataObject, outputPin.Type);
        }
    }

    // Session cleanup
    if (_currentSession.IsCompleted || !string.IsNullOrEmpty(_currentSession.ErrorMessage))
    {
        _currentSession.Dispose();
        _currentSession = null;
    }
}
```

### **Phase 3: Asset Handling Strategy** _(2-3 days)_

**🎯 Goal**: Handle asset downloads for cases where embedded data isn't available

#### **3.1 Simple Asset Download Service**

**File**: `nodetool-sdk/csharp/Nodetool.SDK/Services/AssetDownloadService.cs`

```csharp
public class AssetDownloadService
{
    private readonly INodetoolClient _httpClient;
    private readonly MemoryCache _cache;

    public AssetDownloadService(INodetoolClient httpClient)
    {
        _httpClient = httpClient;
        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 50 });
    }

    // Async asset download with caching
    public async Task<byte[]?> DownloadAssetAsync(string uri)
    {
        var cacheKey = $"asset:{uri}";

        if (_cache.TryGetValue(cacheKey, out byte[]? cached))
            return cached;

        try
        {
            var assetId = ExtractAssetId(uri);
            using var stream = await _httpClient.DownloadAssetAsync(assetId);
            using var memoryStream = new MemoryStream();

            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            _cache.Set(cacheKey, bytes, TimeSpan.FromMinutes(10));
            return bytes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to download asset {uri}: {ex.Message}");
            return null;
        }
    }

    private static string ExtractAssetId(string uri)
    {
        var segments = uri.Split('/');
        var filename = segments.LastOrDefault() ?? "";
        return Path.GetFileNameWithoutExtension(filename);
    }
}
```

## ⏱️ **Implementation Timeline**

- **Week 1**: SDK data object interface + basic type conversion
- **Week 2**: VL type converter service + node integration
- **Week 3**: Asset download service + testing

**Total Duration**: ~3 weeks for complete simplified type handling

## 📊 **Success Criteria**

### **SDK Simplicity**:

- ✅ SDK provides clean `IExecutionSession.GetOutput<T>()` interface
- ✅ All WebSocket complexity hidden from consumers
- ✅ Works identically in VL, Unity, console apps
- ✅ Common type conversions built into SDK

### **VL Integration**:

- ✅ VL nodes only do type conversion (single responsibility)
- ✅ No WebSocket event handling in VL layer
- ✅ No state management in VL layer
- ✅ SKImage, byte[], string conversions work correctly

### **Performance**:

- ✅ Type conversion completes in < 10ms for typical objects
- ✅ Asset caching reduces redundant downloads
- ✅ Memory usage stays reasonable
- ✅ No memory leaks from cached assets

This **dramatically simplified approach** makes VL integration trivial while keeping all complexity in the reusable SDK core!
