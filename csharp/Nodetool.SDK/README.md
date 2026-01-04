# 🎯 Nodetool.SDK - Universal .NET Integration

C# SDK for integrating Nodetool AI workflows into any .NET application.

## 🌟 Features

- **WebSocket Communication**: Real-time workflow execution with progress updates
- **Execution Sessions**: Track job progress and retrieve results asynchronously
- **Asset Management**: Download/upload assets with local caching
- **Type-Safe API**: Strongly typed models matching Nodetool's type system
- **VL/VVVV Integration**: Ready for visual programming environments

## 🚀 Quick Start

### Installation

```xml
<PackageReference Include="Nodetool.SDK" Version="0.1.0" />
```

### Basic Usage - WebSocket Execution (Recommended)

```csharp
using Nodetool.SDK.Execution;
using Nodetool.SDK.Configuration;

// Create execution client (provide explicit endpoints; no library defaults)
var options = new NodeToolClientOptions
{
    WorkerWebSocketUrl = new Uri("ws://localhost:7777/ws"),
    // ApiBaseUrl = new Uri("http://localhost:7777"), // optional (discovery)
    // AuthToken = "...", UserId = "...", ApiUrl = "http://..."
};
using var client = new NodeToolExecutionClient(options);

// Connect to server
await client.ConnectAsync();

// Execute a workflow
var inputs = new Dictionary<string, object>
{
    ["prompt"] = "Generate an image of a sunset",
    ["width"] = 512,
    ["height"] = 512
};

var session = await client.ExecuteWorkflowAsync("workflow-id", inputs);

// Monitor progress
session.ProgressChanged += (progress) => Console.WriteLine($"Progress: {progress:P0}");
session.OutputReceived += (update) =>
{
    Console.WriteLine(
        $"Output {update.NodeName}.{update.OutputName} ({update.OutputType}): {update.Value.ToJsonString()}"
    );
};
session.PreviewReceived += (preview) =>
{
    Console.WriteLine($"Preview from {preview.NodeId}: kind={preview.Value.Kind}");
};

// Wait for completion
bool success = await session.WaitForCompletionAsync();

if (success)
{
    var outputs = session.GetLatestOutputs();
    Console.WriteLine($"Workflow completed with {outputs.Count} outputs");
}
else
{
    Console.WriteLine($"Workflow failed: {session.ErrorMessage}");
}
```

### HTTP API (Synchronous)

```csharp
using Nodetool.SDK.Api;

// Create and configure the client
var client = new NodetoolClient();
client.Configure("http://localhost:8000");

// Get available node types
var nodeTypes = await client.GetNodeTypesAsync();
Console.WriteLine($"Found {nodeTypes.Count} node types");

// Execute a workflow (blocking)
var parameters = new Dictionary<string, object>
{
    ["input_text"] = "Hello, world!"
};

var result = await client.ExecuteWorkflowAsync("my-workflow-id", parameters);
```

### Asset Management

```csharp
using Nodetool.SDK.Assets;

// Create asset manager with caching
var assetManager = new AssetManager();

// Download an asset
string localPath = await assetManager.DownloadAssetAsync(new AssetRef
{
    Type = "image",
    Uri = "https://api.nodetool.ai/api/assets/abc123/download"
});

// Check cache
string? cachedPath = assetManager.GetCachedPath(assetUri);

// Clear cache
assetManager.ClearCache();
```

## Architecture

### Core Components

#### Execution System
- **`INodeToolExecutionClient`**: Main interface for workflow/node execution
- **`NodeToolExecutionClient`**: WebSocket-based implementation
- **`IExecutionSession`**: Track job progress and results
- **`ExecutionSession`**: Session implementation with events

#### Asset System
- **`IAssetManager`**: Asset download/upload with caching
- **`AssetManager`**: Implementation with local file cache

#### HTTP API
- **`INodetoolClient`**: HTTP API interface
- **`NodetoolClient`**: HTTP client implementation

### Message Types

The SDK handles these WebSocket message types:
- `JobUpdate`: Job lifecycle changes (running, completed, failed)
- `NodeUpdate`: Individual node execution status
- `NodeProgress`: Progress for long-running nodes
- `OutputUpdate`: Streaming output values
- `PreviewUpdate`: Streaming preview values (often image-like)

## VL/VVVV Integration

For VL/VVVV Gamma integration, use the `Nodetool.SDK.VL` package which provides:
- **Connect** node: Establish connection to NodeTool server
- **ConnectionStatus** node: Monitor connection state
- Node factory for dynamically created nodes from API metadata

## Type System

| Nodetool Type | C# Type           | Description          |
| ------------- | ----------------- | -------------------- |
| `str`         | `string`          | Text strings         |
| `int`         | `int`             | Integers             |
| `float`       | `float`           | Floating point       |
| `bool`        | `bool`            | Booleans             |
| `list`        | `List<object>`    | Lists                |
| `dict`        | `Dictionary<string, object>` | Dictionaries |
| `image`       | `ImageRef`        | Image references     |
| `audio`       | `AudioRef`        | Audio references     |
| `video`       | `VideoRef`        | Video references     |

## Configuration

### Connection Settings

There are **no hardcoded defaults** in library code. Provide explicit endpoints via `NodeToolClientOptions`.

Override via constructor or configuration:
```csharp
var options = new NodeToolClientOptions { WorkerWebSocketUrl = new Uri("wss://api.nodetool.ai/ws") };
var client = new NodeToolExecutionClient(options, apiKey: "your-api-key");
```

### Asset Cache

Default cache location: `~/.nodetool/cache/assets/`

Override via constructor:
```csharp
var assetManager = new AssetManager("/custom/cache/path");
```

## Contributing

Contributions welcome! See the main Nodetool repository for guidelines.

## License

AGPL-3.0 (same as Nodetool project)
