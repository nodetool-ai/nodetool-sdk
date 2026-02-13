# NodeTool SDK for vvvv

This folder contains VL documents and help patches for using NodeTool from vvvv gamma.

## Build and output

Build from `csharp/`:

```powershell
.\regen-and-verify.ps1 -IncludeVL -SkipGeneration -SkipGitDiff
```

Default output directory:

- `csharp/_vvvv_builds/Release/net8.0/`

You can override output:

```powershell
.\regen-and-verify.ps1 -IncludeVL -OutputDir "C:\path\to\output"
```

## vvvv setup

Use Platform Dependencies and reference:

- `Nodetool.SDK.VL.dll`
- `Nodetool.SDK.dll`
- `Nodetool.Types.dll`

from `csharp/_vvvv_builds/Release/net8.0/`.

## Connection defaults

Electron NodeTool app starts backend on localhost and tries port `7777` first.
If busy, it picks the next free port.

- Worker WebSocket: `ws://127.0.0.1:7777/ws` (or selected port)
- HTTP API: `http://127.0.0.1:7777` (or selected port)

## Help patches

See `vvvv/help/` for example patches and diagnostics.
