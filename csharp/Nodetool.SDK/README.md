# 🎯 Nodetool.SDK - Universal .NET Integration

C# SDK for integrating Nodetool AI workflows into any .NET application.

## WORK IN PROGRESS

## 🌟 Planned Features

- **Universal .NET Support**: currently working on VL integration
- **Type-Safe API**: Strongly typed models matching Nodetool's Python type system
- **Automatic Type Mapping**: Converts Nodetool TypeMetadata to C# types
- **Workflow Execution**: Execute workflows with full input/output typing
- **Node Execution**: Execute individual nodes programmatically
- **Asset Management**: Upload, download, and manage assets

## 🚀 Quick Start

### Installation

```xml
<PackageReference Include="Nodetool.SDK" Version="0.1.0" />
```

### Basic Usage

```csharp
using Nodetool.SDK.Api;
using Nodetool.SDK.Types.Assets;

// Create and configure the client
var client = new NodetoolClient();
client.Configure("http://localhost:8000");

// Get available node types
var nodeTypes = await client.GetNodeTypesAsync();
Console.WriteLine($"Found {nodeTypes.Count} node types");

// Execute a workflow
var parameters = new Dictionary<string, object>
{
    ["input_text"] = "Hello, world!",
    ["temperature"] = 0.7
};

var result = await client.ExecuteWorkflowAsync("my-workflow-id", parameters);
Console.WriteLine($"Workflow result: {result}");

// Work with assets
var imageRef = new ImageRef
{
    Uri = "path/to/image.jpg"
};
```

## Architecture

### Core Components

#### Types System

- **`TypeMetadata`**: Universal type representation (mirrors Python)
- **`BaseType`**: Base class for all Nodetool types with automatic registration
- **Asset Types**: `ImageRef`, `AudioRef`, `VideoRef`, `DocumentRef`, etc.

#### API Client

- **`INodetoolClient`**: Main interface for all API operations
- **`NodetoolClient`**: HTTP client implementation with logging
- **API Models**: Strongly typed request/response models

#### Utilities

- **`TypeMapper`**: Converts TypeMetadata to C# types
- **JSON Serialization**: Custom converters for Nodetool types

### Type Mapping Examples

```csharp
// Python TypeMetadata -> C# Type
TypeMetadata { type: "list", type_args: [{ type: "str" }] }
=> List<string>

TypeMetadata { type: "union", type_args: [{ type: "str" }, { type: "int" }] }
=> object

TypeMetadata { type: "image", optional: true }
=> ImageRef?
```

## Platform Integration

### VL/vvvv Integration

work in progress


## API Reference

### Client Configuration

```csharp
var client = new NodetoolClient();
client.Configure("http://localhost:8000", "optional-api-key");
```

### Node Operations

```csharp
// Get all node types
var nodeTypes = await client.GetNodeTypesAsync();

// Execute a node
var inputs = new Dictionary<string, object> { ["text"] = "Hello" };
var result = await client.ExecuteNodeAsync("text.GenerateText", inputs);
```

### Workflow Operations

```csharp
// List workflows
var workflows = await client.GetWorkflowsAsync();

// Get workflow details
var workflow = await client.GetWorkflowAsync("workflow-id");

// Execute workflow
var params = new Dictionary<string, object> { ["input"] = "value" };
var result = await client.ExecuteWorkflowAsync("workflow-id", params);
```

### Asset Management

```csharp
// Upload asset
using var fileStream = File.OpenRead("image.jpg");
var asset = await client.UploadAssetAsync("image.jpg", fileStream);

// Get asset info
var assetInfo = await client.GetAssetAsync(asset.Id);

// Download asset
using var downloadStream = await client.DownloadAssetAsync(asset.Id);
```

## Type System

The SDK automatically maps Nodetool's Python type system to C#:

| Python Type | C# Type           | Description          |
| ----------- | ----------------- | -------------------- |
| `str`       | `string`          | Text strings         |
| `int`       | `int`             | Integers             |
| `float`     | `double`          | Floating point       |
| `bool`      | `bool`            | Booleans             |
| `list[T]`   | `List<T>`         | Generic lists        |
| `dict[K,V]` | `Dictionary<K,V>` | Generic dictionaries |
| `image`     | `ImageRef`        | Image references     |
| `audio`     | `AudioRef`        | Audio references     |
| `video`     | `VideoRef`        | Video references     |


## Future Enhancements

- **Code Generation**: Automatic C# type generation from Python
- **WebSocket Support**: Real-time streaming for long-running workflows
- **Plugin System**: Custom type extensions
- **Performance Optimizations**: Caching and batching

## Contributing

This SDK is designed to be the foundation for all .NET integrations with Nodetool. Contributions welcome!

## License

Same as Nodetool project (AGPL-3.0)

---
