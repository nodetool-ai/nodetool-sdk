# Nodetool.SDK.VL

VL/vvvv integration layer for `Nodetool.SDK`.

This package focuses on:
- a shared connection to a NodeTool server (WebSocket worker + HTTP discovery)
- dynamically generated VL nodes for workflow execution

## Installation

Add a reference to the NuGet package:

```xml
<PackageReference Include="Nodetool.SDK.VL" Version="0.1.0" />
```

## Usage (overview)

- Use the **Connect** node to set the server URL and optional auth token.
- Use the generated **Workflow** nodes to execute workflows by name and map outputs to pins.

### Local defaults

Typical local NodeTool endpoints:
- Worker WS: `ws://localhost:7777/ws`
- HTTP API: `http://localhost:7777`

The VL layer derives the HTTP base URL from the worker URL automatically.


