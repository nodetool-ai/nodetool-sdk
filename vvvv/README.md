# NodeTool SDK for vvvv

http://nodetool.ai/
Execute NodeTool nodes and workflows from vvvv gamma.

## vvvv setup

- Supported host: vvvv gamma 7.1 / `VL.Core` 2025.7.1
- Install: `nuget install VL.Nodetool -Version 0.1.6`
- Open `help/Nodetool_Help.vl`

## Connection

NodeTool starts the backend service on localhost and uses port `7777`.
BaseURL: `ws://localhost:7777`

SDK workflow discovery and lifecycle preflight are enabled by default. Start
the NodeTool development server normally:

```bash
npm run dev
```

The package targets .NET 8 and NodeTool workflow-interface/lifecycle protocol
v1. Server administrators can disable those additive SDK surfaces with the
documented emergency kill switches; clients cannot override server policy.

## Current limitations

- The first release is verified against vvvv gamma 7.1 and `VL.Core`
  2025.7.1; broader host-version coverage is not yet claimed.
- Interactive media, reload, reconnect, and long-running soak coverage remains
  part of the refinement matrix.
- The package does not support older NodeTool workflow contracts.
