# Nodetool.SDK.VL

VL/vvvv integration layer for `Nodetool.SDK`.

This package provides:

- a shared connection to a NodeTool server (WebSocket worker + HTTP discovery)
- dynamically generated VL nodes for workflows and nodes

## Build in this repo

From `nodetool-sdk/csharp/`:

```powershell
.\regen-and-verify.ps1 -IncludeVL -SkipGeneration -SkipGitDiff
```

Default output directory:

- `csharp/_vvvv_builds/Release/net8.0/`

Optional custom output:

```powershell
.\regen-and-verify.ps1 -IncludeVL -OutputDir "C:\path\to\output"
```

## Installation

Add a reference to the NuGet package:

```xml
<PackageReference Include="Nodetool.SDK.VL" Version="0.1.1" />
```

## Usage (overview)

### vvvv gamma: how to reference this

In vvvv gamma, reference either:

- `Nodetool.SDK.VL.csproj` directly (good for development), or
- the built DLLs directly, or the release
- nuget install VL.Nodetool

DLLs:

- `nodetool-sdk\csharp\Nodetool.SDK.VL\bin\Release\Nodetool.SDK.VL.dll`
- `nodetool-sdk\csharp\Nodetool.SDK.VL\bin\Release\Nodetool.SDK.dll`
- `nodetool-sdk\csharp\Nodetool.SDK.VL\bin\Release\Nodetool.Types.dll`

### Connect node

In the node browser:

- `Nodetool -> Connect`

Local default endpoint:

- BaseUrl (worker WS): `ws://localhost:7777`

Execution timeout:

- `ExecutionTimeoutSeconds` sets the shared default for workflow and single-node runs (default: 300 seconds).
- Each generated execution node has its own `ExecutionTimeoutSeconds` input. Leave it at `0` to inherit the Connect-node default, or set a positive value for that node.
- Values are capped at 86400 seconds (24 hours).

Connection recovery:

- `AutoReconnect` is enabled by default. After an unexpected socket disconnect, the SDK retries the connection with bounded exponential backoff.
- Active workflow sessions send `reconnect_job` after the socket returns and poll the server's persisted job state until a terminal update arrives.
- Turning `AutoReconnect` off only disables automatic retries; the Connect node's `Reconnect` pulse remains available.
- An intentional Disconnect does not trigger automatic reconnection.

Workflow discovery transport:

- HTTP remains the default bootstrap transport, so loading the package does not require an open execution socket.
- Enable `UseWebSocketDiscovery` on the Connect node to switch workflow summaries and interface batches to correlated MessagePack RPC after the shared socket connects.
- When the socket first connects, the workflow factory requests a refresh. A later disconnect keeps the last successful workflow nodes available.
- Connection changes retain the last workflow factory while the replacement is fetched. A single empty discovery response is treated as provisional and must be confirmed before existing workflow nodes are removed.

Media transport:

- `InlineMediaLimitBytes` defaults to 4 MiB. Larger local media is uploaded through the asset API and represented by `asset_id` during execution.
- Set the limit to `0` to upload all binary media, or raise it up to 64 MiB when inline transport is preferable.
- Workflow audio, video, document, and generic asset pins use the typed SDK asset-reference classes (`AudioRef`, `VideoRef`, `DocumentRef`, and `GenericAssetRef`).
- Image pins use `SKImage`. A workflow node owns images it produces and disposes them when replaced or when the node is disposed; downstream patches should treat output images as borrowed and must not dispose them. Input images remain owned by the caller.
- Workflow outputs are latched and reapplied on each VL update so event-driven scalar and media results remain visible after the execution frame.
- Structured workflow pins use generated `Nodetool.Types` DTOs when the interface discriminator or `type_name` resolves in the SDK registry. Unknown structures remain explicit `Object` fallback pins.

### Workflow nodes

Generated from the authoritative workflow-interface v1 contract, over compact HTTP by default or correlated WebSocket RPC when enabled on the Connect node.

Category:

- `Nodetool Workflows`

Discovery diagnostics:

- `Nodetool Workflows -> WorkflowAPIStatus` reports the last successful UTC refresh, server version, authoritative interface source, workflow count, and last discovery error.
- Pulse `Refresh` to request another compact discovery pass. Unchanged interfaces are reused by workflow revision, node-registry revision, and interface etag.
- If a refresh fails after a successful load, the existing workflow nodes stay available and `Status` reports that their metadata is stale.

### Node nodes

Generated from `GET /api/nodes/metadata`.

Category:

- `Nodetool Nodes.*`

### Image helpers

- `Nodetool -> DecodeImageRef`
- `Nodetool.Images -> DecodeImageRefToSKImage`
