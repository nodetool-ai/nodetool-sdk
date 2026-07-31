# NodeTool SDK

Portable **C#** client library and host integrations for running [NodeTool](https://nodetool.ai) AI workflows and nodes from other applications.

The core is [`Nodetool.SDK`](csharp/Nodetool.SDK/) — a .NET 8 library for discovery, execution, assets, and streaming. Host adapters build on top of it; the first shipping integration is **vvvv gamma** via [`VL.Nodetool`](vvvv/).

## Overview

NodeTool is the authoring environment and execution backend. A host application connects to that backend, discovers workflows and nodes, and submits runs while wiring data through its own types and UI. Workflows can mix **local models** (on your machine) and **cloud/API providers** — that choice is made in NodeTool when authoring, not in the host.

```
NodeTool backend  ←→  Nodetool.SDK  ←→  host adapter (vvvv, custom .NET, …)
       ↑
 workflows, nodes, jobs
```

Requires **NodeTool 0.7.0-rc.32** or newer ([download](https://nodetool.ai)).

## VL.Nodetool for vvvv gamma

[`VL.Nodetool`](vvvv/) runs NodeTool workflows and nodes from vvvv gamma. Install with `nuget install VL.Nodetool -Version 0.1.6`, start NodeTool, add a **Nodetool → Connect** node, then browse **Nodetool Workflows** and **Nodetool Nodes** in the node menu.

See VL SDK: **[vvvv/README.md](vvvv/README.md)**


## C# SDK

[`Nodetool.SDK`](csharp/Nodetool.SDK/) is host-agnostic: HTTP and MessagePack WebSocket against NodeTool, with no vvvv or engine dependencies. Use it for custom .NET tools, services, or new host adapters.

```csharp
using Nodetool.SDK.Connection;

await using var connection = new NodeToolConnectionManager(new NodeToolConnectionProfile
{
    ServerUrl = new Uri("http://127.0.0.1:7777"),
    TokenProvider = new StaticNodeToolTokenProvider("local")
});

var execution = await connection.GetConnectedClientAsync();
var workflows = await execution.GetWorkflowSummariesAsync();
```

Full API and usage: [`csharp/Nodetool.SDK/README.md`](csharp/Nodetool.SDK/README.md)

### Local connection defaults

When using the NodeTool desktop app, the backend binds to localhost and selects port `7777` by default (next free port if occupied):

- WebSocket: `ws://127.0.0.1:<port>/ws`
- HTTP API: `http://127.0.0.1:<port>`

### Asset I/O

Portable asset services in `Nodetool.SDK.Assets`:

- `AssetUploader` — upload local files, streams, or bytes as temporary execution inputs or persistent NodeTool assets
- `AssetMaterializer` — resolve typed `AssetRef` values to identity-addressed local cache files
- `AssetSaver` — materialize and atomically copy an asset to a caller-selected destination

Host adapters project their own path, image, texture, and audio types around this layer.

### Realtime streaming

The C# base normalizes NodeTool's `output_update` chunk values and job-scoped `chunk` messages as `ExecutionStreamUpdate`. Active execution sessions can stream inputs, end input streams, and update running-node properties. Realtime audio can be validated as `AudioStreamChunk` and fed into `AudioStreamBuffer` or `AudioStreamPlaybackBuffer` for non-blocking, allocation-free playback reads with sample-rate and channel conversion.

Workflow discovery preserves the server-declared output `stream_kind`, so host adapters do not need to guess whether a generic chunk contains text, audio, or control data.

Realtime consumers must use `WorkflowEventDetail.Outputs`. `WorkflowEventDetail.Terminal` intentionally withholds intermediate chunks.

## Building

From `csharp/`:

```powershell
.\regen-and-verify.ps1 -SkipGeneration -SkipGitDiff -VerifySdkPackage
```

Include the vvvv adapter and pack `VL.Nodetool`:

```powershell
.\regen-and-verify.ps1 -IncludeVL -SkipGeneration -SkipGitDiff -VerifySdkPackage
```

For an already-restored or offline workspace, add `-NoRestore`. Verification stops immediately if a `dotnet` build, test, or pack command fails.

Default VL build output: `csharp/_vvvv_builds/Release/net8.0/`

Override output folder:

```powershell
.\regen-and-verify.ps1 -IncludeVL -OutputDir "C:\path\to\output"
```

Pack the vvvv NuGet package from `vvvv/deployment/` — see [`vvvv/README.md`](vvvv/README.md).

## Repository layout

| Path | Purpose |
|------|---------|
| [`csharp/Nodetool.SDK/`](csharp/Nodetool.SDK/) | Portable .NET 8 client — connection, discovery, execution, assets, streaming |
| [`csharp/Nodetool.Types/`](csharp/Nodetool.Types/) | Generated node metadata and typed DTOs |
| [`csharp/Nodetool.SDK.VL/`](csharp/Nodetool.SDK.VL/) | vvvv gamma adapter — dynamic node factories, type mapping, media helpers |
| [`vvvv/`](vvvv/) | `VL.Nodetool` NuGet package, help patches, and vvvv user docs |