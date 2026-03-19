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

### Workflow nodes

Generated from `GET /api/workflows/`.

Category:

- `Nodetool Workflows`

### Node nodes

Generated from `GET /api/nodes/metadata`.

Category:

- `Nodetool Nodes.*`

### Image helpers

- `Nodetool -> DecodeImageRef`
- `Nodetool.Images -> DecodeImageRefToSKImage`
