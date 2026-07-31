# NodeTool SDK for vvvv

[NodeTool](https://nodetool.ai/) integration for **vvvv gamma** — run AI workflows and individual nodes from VL while the local NodeTool backend orchestrates execution (local models, cloud/API providers, or both).

## Overview

NodeTool is where you author workflows and configure nodes. vvvv is where you patch, trigger runs, and wire results into your realtime graph.

The `VL.Nodetool` package connects vvvv to a running NodeTool backend over localhost. It discovers your workflows and node catalog, generates matching VL nodes dynamically, and submits runs to that backend. Individual workflow nodes may use local models or cloud/API providers, depending on how the workflow is built in NodeTool. Input and output pins on a workflow node reflect the workflow's public interface as defined there.

**NodeTool must be running** for both discovery and execution. If the backend is not reachable, the Connect node reports an error and workflow/node entries will not appear.

## Quick start

### 1. Start NodeTool

Download [NodeTool 0.7.0-rc.32 or newer](https://nodetool.ai/studio) and launch it. The app starts a local backend on port `7777` by default.

When you close the window, choose **Keep Running in Background** if you want vvvv to keep using the backend without the UI open. The system tray icon confirms the server is still running.

Workflow and node discovery work automatically — just start the NodeTool desktop app. No dev server or extra setup required.

SDK workflow discovery and lifecycle preflight are enabled by default.

### 2. Install the vvvv package

Supported host: **vvvv gamma 7.1** / `VL.Core` 2025.7.1

In vvvv, open **Manage NuGets → Commandline** and run:

```text
nuget install VL.Nodetool -Version 0.1.6
```

Open the included help patch: `help/Nodetool_Help.vl`

### 3. Connect

Add a **Nodetool → Connect** node to your patch.

| Setting | Default | Notes |
|---------|---------|-------|
| BaseUrl | `ws://localhost:7777` | WebSocket worker endpoint |
| ApiKey | `local` | Default token for local desktop use |
| LoadWorkflows | enabled | Populates **Nodetool Workflows** in the node menu |
| LoadNodes | enabled | Populates **Nodetool Nodes** in the node menu |

Confirm **Status** shows connected before adding workflow or node instances.

### 4. Use workflows and nodes

Once connected, open the node menu (double-click or right-click in a patch):

- **Nodetool Workflows** — one generated node per workflow in your NodeTool workspace
- **Nodetool Nodes** — individual NodeTool nodes (grouped by provider/category)

Each workflow or node execution input is named **Run**. Connect your vvvv logic, set parameter pins, and trigger a run.

### Model manager extension (preview)

The package includes an optional **Nodetool Models** editor extension. Open it
from the vvvv extension menu (shortcut: `Alt+M`). The current preview is the
first validation surface for the standard dockable HDE/ImGui window lifecycle;
live catalog rows and download actions will be connected after the interactive
spike is accepted. The extension is editor-only and is not required for
generated nodes, workflow execution, or exported applications.

## Workflow inputs and outputs

A workflow's vvvv pins come from its **public interface** in NodeTool:

- Drag a connection from a node pin on the canvas — the popup menu offers **Input** and **Output** nodes. Place one to expose a parameter or result; it appears as a pin on the generated vvvv workflow node (types, defaults, and documentation are carried over).
- Typed output nodes (image, audio, video, etc.) work the same way when you need a specific result type.

After you change inputs or outputs in NodeTool, save the workflow and pulse **RefreshDiscovery** on the Connect node (or reconnect). To see updated pins in vvvv, add a **new** workflow node from the node menu — an existing instance may keep its old pin layout even after discovery refreshes.

Media inputs accept vvvv `Path` pins for file-backed assets. Image pins use `SKImage`. Workflow outputs are latched so results remain visible after the run completes. Outputs declared with `stream_kind: audio` also expose a companion **Audio Source** pin for VL.Audio playback.

## Connection details

Local desktop defaults:

- WebSocket worker: `ws://localhost:7777`
- HTTP API: `http://localhost:7777` (used for catalog bootstrap and asset operations)

The package targets .NET 8. Pair **`VL.Nodetool` 0.1.6** with **NodeTool 0.7.0-rc.32** or newer.

Useful Connect-node options:

- **AutoReconnect** (default on) — retries after unexpected disconnects; active jobs can reconnect via `reconnect_job`
- **RefreshDiscovery** — refresh workflow and node catalogs without restarting vvvv
- **UseWebSocketDiscovery** — switch catalog fetching to MessagePack RPC over the open socket (HTTP is the default bootstrap transport)
- **EventDetail** — `Outputs` for low-overhead interactive use; `Full` for complete execution visibility; `Terminal` for final results only

Further adapter details: [`csharp/Nodetool.SDK.VL/README.md`](../csharp/Nodetool.SDK.VL/README.md)

## Development from source

If you are working in this repository rather than installing from NuGet, build the VL assemblies from `csharp/`:

```powershell
.\regen-and-verify.ps1 -IncludeVL -SkipGeneration -SkipGitDiff
```

Pack and verify the NuGet package:

```powershell
cd vvvv\deployment
.\pack-and-verify.ps1
```

## Current limitations

- Tested with **vvvv gamma 7.1** / `VL.Core` **2025.7.1** and **`VL.Nodetool` 0.1.6**. Other vvvv versions are not supported yet.
- Requires **NodeTool 0.7.0-rc.32** or newer ([download](https://nodetool.ai/studio)). Older desktop builds do not expose the SDK workflow API this package uses.
- NodeTool version 0.7.1 or later
- Some scenarios are still being tested in depth: interactive media workflows, reconnecting after NodeTool restarts, and patches left running for many hours. Please report anything that misbehaves.

## Links

- [NodeTool](https://nodetool.ai/)
- [NodeTool repository](https://github.com/nodetool-ai/nodetool)
- [SDK repository (vvvv)](https://github.com/nodetool-ai/nodetool-sdk/tree/main/vvvv)
