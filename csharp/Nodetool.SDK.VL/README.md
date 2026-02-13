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
- the built DLLs directly (most reliable in vvvv gamma).

Important: vvvv gamma does not always follow `ProjectReference` dependencies reliably. If you see type-load issues with csproj references, use DLL references.

Recommended DLLs:

- `csharp/_vvvv_builds/Release/net8.0/Nodetool.SDK.VL.dll`
- `csharp/_vvvv_builds/Release/net8.0/Nodetool.SDK.dll`
- `csharp/_vvvv_builds/Release/net8.0/Nodetool.Types.dll`

### Connect node

In the node browser:

- `Nodetool -> Connect`

Local default endpoint:

- BaseUrl (worker WS): `ws://localhost:7777/ws` (or `ws://localhost:7777`)

### Workflow nodes

Generated from `GET /api/workflows/`.

Category:

- `Nodetool Workflows`

Diagnostics:

- `WorkflowAPIStatus`

### Node nodes

Generated from `GET /api/nodes/metadata`.

Category:

- `Nodetool Nodes.*`

Diagnostics:

- `NodesAPIStatus`

### Image helpers

- `Nodetool -> DecodeImageRef`
- `Nodetool.Images -> DecodeImageRefToSKImage`

## URL behavior

Set worker URL on `Connect -> BaseUrl`. The VL layer derives HTTP API URL automatically by converting ws/wss to http/https.

Typical local defaults:

- Worker WS: `ws://localhost:7777/ws`
- HTTP API: `http://localhost:7777`
