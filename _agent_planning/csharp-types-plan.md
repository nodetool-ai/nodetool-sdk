# NodeTool C# Type System Enhancement Plan

## 🎯 **Current Status**

✅ **Core Type Infrastructure** - `BaseType`, `TypeMetadata`, type registry  
✅ **Basic Asset Types** - `ImageRef`, `AudioRef`, `VideoRef`, etc.  
✅ **Type Mapping Utilities** - `TypeMapper` for metadata to C# types  
✅ **VL Type Integration** - Basic mapping in factories  
❌ **WebSocket Data Object Handling** - Need real implementation  
❌ **Asset Conversion Pipeline** - Upload/download with caching

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

## 📋 **Implementation Plan: Real Data Object Handling**

### **Phase 1: Data Object Models** _(1-2 days)_

**🎯 Goal**: Implement the real `NodeToolDataObject` format from WebSocket messages

#### **1.1 Core Data Object Model**

**File**: `nodetool-sdk/csharp/Nodetool.SDK/Models/NodeToolDataObject.cs`

```csharp
public class NodeToolDataObject
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("uri")]
    public string? Uri { get; set; }

    [JsonPropertyName("asset_id")]
    public string? AssetId { get; set; }

    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }

    // Type-specific metadata
    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("duration")]
    public double? Duration { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("size")]
    public long? Size { get; set; }

    // Type checks
    public bool IsImage => Type == "image";
    public bool IsAudio => Type == "audio";
    public bool IsVideo => Type == "video";
    public bool IsText => Type == "text";

    // Data availability checks
    public bool HasEmbeddedData => Data.HasValue && Data.Value.ValueKind != JsonValueKind.Null;
    public bool HasAssetReference => !string.IsNullOrEmpty(Uri) || !string.IsNullOrEmpty(AssetId);

    // Extract embedded data
    public T? GetEmbeddedData<T>()
    {
        if (HasEmbeddedData)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(Data.Value.GetRawText());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to extract embedded data: {ex.Message}");
            }
        }
        return default;
    }
}
```

### **Phase 2: VL Type Conversion** _(2-3 days)_

**🎯 Goal**: Convert `NodeToolDataObject` to appropriate VL types (SKImage, byte[], etc.)

#### **2.1 VL Data Object Converter**

**File**: `nodetool-sdk/csharp/Nodetool.SDK.VL/Services/VLDataObjectConverter.cs`

```csharp
public class VLDataObjectConverter
{
    private readonly AssetDownloadService _assetService;
    private readonly ILogger _logger;

    public VLDataObjectConverter(AssetDownloadService assetService, ILogger? logger = null)
    {
        _assetService = assetService;
        _logger = logger ?? new NullLogger();
    }

    public async Task<object?> ConvertToVLTypeAsync(NodeToolDataObject dataObject, Type targetVLType)
    {
        if (dataObject == null) return null;

        try
        {
            // Handle VL-specific type conversions
            if (targetVLType == typeof(SKImage) && dataObject.IsImage)
            {
                return await ConvertToSKImage(dataObject);
            }

            if (targetVLType == typeof(byte[]) && (dataObject.IsAudio || dataObject.IsVideo))
            {
                return await ConvertToByteArray(dataObject);
            }

            if (targetVLType == typeof(string))
            {
                return ConvertToString(dataObject);
            }

            // Fallback: JSON representation
            return JsonSerializer.Serialize(dataObject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to convert {dataObject.Type} to {targetVLType.Name}");
            return GetDefaultValue(targetVLType);
        }
    }

    private async Task<SKImage?> ConvertToSKImage(NodeToolDataObject dataObject)
    {
        // Priority 1: Embedded data
        if (dataObject.HasEmbeddedData)
        {
            var embeddedData = dataObject.GetEmbeddedData<object>();

            if (embeddedData is string base64)
            {
                try
                {
                    var bytes = Convert.FromBase64String(base64);
                    return SKImage.FromEncodedData(bytes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to decode embedded base64 image");
                }
            }
        }

        // Priority 2: Asset reference
        if (dataObject.HasAssetReference)
        {
            try
            {
                var imageBytes = await _assetService.DownloadBytesAsync(dataObject.Uri ?? "");
                if (imageBytes != null)
                {
                    return SKImage.FromEncodedData(imageBytes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to download image asset: {dataObject.Uri}");
            }
        }

        return null;
    }

    private async Task<byte[]?> ConvertToByteArray(NodeToolDataObject dataObject)
    {
        // Priority 1: Embedded data
        if (dataObject.HasEmbeddedData)
        {
            var embeddedData = dataObject.GetEmbeddedData<object>();

            if (embeddedData is string base64)
            {
                try
                {
                    return Convert.FromBase64String(base64);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to decode embedded base64 data");
                }
            }
        }

        // Priority 2: Asset reference
        if (dataObject.HasAssetReference)
        {
            try
            {
                return await _assetService.DownloadBytesAsync(dataObject.Uri ?? "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to download asset: {dataObject.Uri}");
            }
        }

        return null;
    }

    private string ConvertToString(NodeToolDataObject dataObject)
    {
        // For text data, extract embedded content first
        if (dataObject.IsText && dataObject.HasEmbeddedData)
        {
            var text = dataObject.GetEmbeddedData<string>();
            if (!string.IsNullOrEmpty(text))
                return text;
        }

        // Fallback: JSON representation
        return JsonSerializer.Serialize(dataObject, new JsonSerializerOptions { WriteIndented = true });
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

### **Phase 3: Asset Download Service** _(2-3 days)_

**🎯 Goal**: Handle asset downloads with caching for performance

#### **3.1 Asset Download Service**

**File**: `nodetool-sdk/csharp/Nodetool.SDK/Services/AssetDownloadService.cs`

```csharp
public class AssetDownloadService
{
    private readonly INodetoolClient _httpClient;
    private readonly MemoryCache _cache;
    private readonly ILogger _logger;

    public AssetDownloadService(INodetoolClient httpClient, ILogger? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger ?? new NullLogger();
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 100 // Max 100 cached assets
        });
    }

    public async Task<byte[]?> DownloadBytesAsync(string uri)
    {
        if (string.IsNullOrEmpty(uri)) return null;

        // Check cache first
        var cacheKey = $"bytes:{uri}";
        if (_cache.TryGetValue(cacheKey, out byte[]? cached))
        {
            _logger.LogDebug($"Asset cache hit: {uri}");
            return cached;
        }

        try
        {
            _logger.LogDebug($"Downloading asset: {uri}");

            // Extract asset ID from URI and download
            var assetId = ExtractAssetId(uri);
            using var stream = await _httpClient.DownloadAssetAsync(assetId);
            using var memoryStream = new MemoryStream();

            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            // Cache the result
            _cache.Set(cacheKey, bytes, TimeSpan.FromMinutes(30));
            _logger.LogDebug($"Downloaded and cached asset: {uri} ({bytes.Length} bytes)");

            return bytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to download asset: {uri}");
            return null;
        }
    }

    private static string ExtractAssetId(string uri)
    {
        // Extract asset ID from NodeTool URI format
        // Example: "http://localhost:8000/api/storage/abc123.png" → "abc123"
        var segments = uri.Split('/');
        var filename = segments.LastOrDefault() ?? "";
        return Path.GetFileNameWithoutExtension(filename);
    }

    public void ClearCache()
    {
        _cache.Dispose();
    }
}
```

### **Phase 4: Integration with Execution** _(1-2 days)_

**🎯 Goal**: Integrate data object conversion with WebSocket execution results

#### **4.1 Update Workflow Execution Session**

**File**: Update `nodetool-sdk/csharp/Nodetool.SDK/Services/WebSocketWorkflowExecutionService.cs`

```csharp
public void HandleOutputUpdate(OutputUpdateMessage message)
{
    _logger.LogDebug($"Output update: {message.OutputName} from {message.NodeName}");

    var dataObject = message.GetDataObject();
    if (dataObject != null)
    {
        // Store the raw data object for later conversion
        _realtimeOutputs[message.OutputName] = dataObject;

        var logEntry = $"Output '{message.OutputName}' updated: {dataObject.Type}";
        if (dataObject.HasAssetReference)
            logEntry += $" (Asset: {dataObject.AssetId})";
        if (dataObject.HasEmbeddedData)
            logEntry += " (Embedded data)";

        _executionLogs.Add(logEntry);
    }

    ReportProgress("running", $"Generated output: {message.OutputName}");
}
```

#### **4.2 Update VL Workflow Node**

**File**: Update `nodetool-sdk/csharp/Nodetool.SDK.VL/Nodes/WorkflowNodeBase.cs`

```csharp
private async Task SetOutputsFromResult(WebSocketWorkflowResult result)
{
    var converter = VLServiceLocator.GetService<VLDataObjectConverter>();

    foreach (var kvp in _outputPins.Where(p => p.Key != "IsRunning" && p.Key != "Error"))
    {
        if (result.Outputs.TryGetValue(kvp.Key, out var dataObject))
        {
            // Convert NodeToolDataObject to appropriate VL type
            var vlValue = await converter.ConvertToVLTypeAsync(dataObject, kvp.Value.Type);
            kvp.Value.Value = vlValue;
        }
    }
}
```

## ⏱️ **Implementation Timeline**

- **Week 1**: Data object models + VL conversion logic
- **Week 2**: Asset download service + caching
- **Week 3**: Integration with execution + testing

**Total Duration**: ~3 weeks for production-ready data object handling

## 📊 **Success Metrics**

### **Data Handling**:

- ✅ All NodeTool data objects convert correctly to VL types
- ✅ Embedded data takes priority over asset references
- ✅ SKImage conversion works for all image formats
- ✅ Asset download caching reduces redundant requests by 80%+

### **Performance**:

- ✅ Data conversion completes in < 100ms for typical objects
- ✅ Memory usage stays reasonable with asset caching
- ✅ No memory leaks from cached assets
- ✅ Large assets (>10MB) download successfully

### **Reliability**:

- ✅ Graceful degradation when assets fail to download
- ✅ Fallback to JSON representation for unknown types
- ✅ Thread-safe asset caching
- ✅ Clear error messages for conversion failures

## 🔗 **Integration Points**

- **Supports**: WebSocket workflow and node execution
- **Provides**: VL-ready data conversion from NodeTool objects
- **Requires**: SkiaSharp for SKImage handling
- **Caches**: Assets to improve performance

This plan focuses on the **real data object format** discovered from WebSocket execution instead of speculative type mappings.
