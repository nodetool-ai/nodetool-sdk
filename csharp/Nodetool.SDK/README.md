# Nodetool.SDK

.NET 8 client library for discovering and executing NodeTool workflows and
nodes over the current HTTP and MessagePack WebSocket APIs.

## Installation

```xml
<PackageReference Include="Nodetool.SDK" Version="0.1.1" />
```

The package declares its required `Nodetool.Types` dependency and loads its
generated DTOs during type discovery. Applications do not need to reference or
touch that package separately.

## WebSocket execution

```csharp
using Nodetool.SDK.Configuration;
using Nodetool.SDK.Execution;

var options = new NodeToolClientOptions
{
    WorkerWebSocketUrl = new Uri("ws://127.0.0.1:7777/ws"),
    ApiBaseUrl = new Uri("http://127.0.0.1:7777"),
    AutoReconnect = true
};

await using var client = new NodeToolExecutionClient(options);
if (!await client.ConnectAsync())
    throw new InvalidOperationException(client.LastError ?? "Connection failed.");

var session = await client.ExecuteWorkflowAsync(
    "workflow-id",
    new Dictionary<string, object>
    {
        ["prompt"] = "hello from C#"
    });

session.OutputReceived += update =>
{
    Console.WriteLine(
        $"{update.NodeName}.{update.OutputName}: {update.Value.ToJsonString()}");
};

var succeeded = await session.WaitForCompletionAsync();
if (!succeeded)
    Console.WriteLine(session.ErrorMessage);
```

`WaitForCompletionAsync` returns `false` when the remote job fails or is
cancelled. Cancelling its token cancels only the local wait and throws
`OperationCanceledException`. Call `session.CancelAsync()` to cancel the remote
job.

The execution client supports:

- workflow execution by ID or case-insensitive name;
- single-node and explicit-graph execution;
- output, preview, node, progress, and completion events;
- queued and running job cancellation;
- reconnecting active sessions;
- compact workflow-summary and authoritative workflow-interface discovery;
- full node metadata and recursive node-type inventory discovery.

## HTTP API

Use the HTTP client for health checks, metadata, workflow interfaces, assets,
and the synchronous execution routes:

```csharp
using Nodetool.SDK.Api;

using var client = new NodetoolClient(new Uri("http://127.0.0.1:7777"));

var health = await client.GetHealthAsync();
var summaries = await client.GetWorkflowSummariesAsync();
var interfaces = await client.GetWorkflowInterfacesAsync(
    summaries.Take(20).Select(workflow => workflow.Id).ToArray());
var nodeTypes = await client.GetNodeTypeInventoryAsync(cursor: 0, limit: 100);
```

`NodetoolClient(Uri, ...)` is the preferred constructor because the endpoint is
explicit. The parameterless constructor retains the localhost development
endpoint for convenience.

When a caller injects an `HttpClient`, disposing `NodetoolClient` does not
dispose that externally owned instance.

## Assets

The asset manager uses the canonical typed references from
`Nodetool.SDK.Types.Assets`:

```csharp
using Nodetool.SDK.Api;
using Nodetool.SDK.Assets;
using Nodetool.SDK.Types.Assets;

using var api = new NodetoolClient(new Uri("http://127.0.0.1:7777"));
using var assets = new AssetManager(nodetoolClient: api);

AssetRef uploaded = await assets.UploadAssetAsync(
    @"C:\media\sample.wav",
    "audio/wav");

var audio = (AudioRef)uploaded;
Console.WriteLine(audio.Uri);

var localPath = await assets.DownloadAssetAsync(audio);
```

Uploads preserve their MIME content type and return `ImageRef`, `AudioRef`,
`VideoRef`, `DocumentRef`, or `GenericAssetRef` as appropriate. Downloads
support HTTP(S), data URIs, file URIs, and existing local paths.

The default asset cache is:

```text
%USERPROFILE%\.nodetool\cache\assets
```

An injected `HttpClient` remains owned by the caller. An internally created
client is disposed with `AssetManager`.

## NodeTool type mapping

| NodeTool type | C# representation |
| --- | --- |
| `str`, `text` | `string` |
| `int` | `int` |
| `float` | `float` |
| `bool` | `bool` |
| `list` | typed collection or `List<object>` |
| `dict`, `object` | `Dictionary<string, object>` or generated DTO |
| `image` | `ImageRef` |
| `audio` | `AudioRef` |
| `video` | `VideoRef` |
| `document` | `DocumentRef` |
| `asset` | `GenericAssetRef` |

Dynamic values received during execution are exposed as `NodeToolValue`, which
preserves scalar, binary, list, map, and typed-reference shapes without forcing
callers through JSON strings.

## Test console

```bash
cd /m/P/NODETOOL/____REPOS____/nodetool-sdk/csharp/Nodetool.SDK
dotnet run -c Release --project ./TestConsole/Nodetool.SDK.TestConsole.csproj -- \
  run-workflow \
  --ws http://127.0.0.1:7777/ws \
  --http http://127.0.0.1:7777 \
  --workflow "SDK Test - Primitives"
```

## Build and package verification

From `csharp/`:

```powershell
.\regen-and-verify.ps1 `
  -SkipGeneration `
  -SkipGitDiff `
  -NoRestore `
  -IncludeVL `
  -IncludeVLTests `
  -VerifySdkPackage `
  -VerifyVLPackage
```

`-VerifySdkPackage` packs both base packages and verifies that
`Nodetool.SDK.nuspec` declares the `Nodetool.Types` dependency required by its
public API. Use `-NoRestore` only after restoring the solution; any failed
build, test or pack command terminates verification.

## VL/vvvv

Use the separate `Nodetool.SDK.VL` / `VL.Nodetool` package for vvvv gamma
integration, dynamic node factories, native `Path` media inputs, and
`Spread<T>` collections.

## License

AGPL-3.0-only.
