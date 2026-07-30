# Nodetool SDK

SDK and integration for NodeTool clients.

## C# and VL builds

From `csharp/`:

```powershell
.\regen-and-verify.ps1 -IncludeVL -SkipGeneration -SkipGitDiff -VerifySdkPackage
```

For an already-restored or offline workspace, add `-NoRestore`. Verification
stops immediately if a `dotnet` build, test or pack command fails.

Default build output:

- `csharp/_vvvv_builds/Release/net8.0/`

Override output folder:

```powershell
.\regen-and-verify.ps1 -IncludeVL -OutputDir "C:\path\to\output"
```

## Electron local connection default

When using the NodeTool Electron app, backend binds to localhost and selects port `7777` by default (next free port if occupied):

- WebSocket: `ws://127.0.0.1:<port>/ws`
- HTTP API: `http://127.0.0.1:<port>`

## C# asset I/O

Portable asset services live in `Nodetool.SDK.Assets`:

- `AssetUploader` uploads local files, streams, or bytes as temporary
  execution inputs or persistent NodeTool assets.
- `AssetMaterializer` resolves typed `AssetRef` values to identity-addressed
  local cache files.
- `AssetSaver` materializes and atomically copies an asset to a
  caller-selected destination, with explicit overwrite behavior.

These services contain no vvvv or Unity types. Host adapters should project
their own path, image, texture, audio, and trigger types around this layer.

## Portable realtime streaming

The C# base normalizes NodeTool's `output_update` chunk values and standalone
job-scoped `chunk` messages as `ExecutionStreamUpdate`. Active execution
sessions can stream inputs, end an input stream, and update running-node
properties. Realtime audio can be validated as `AudioStreamChunk` and fed into
the fixed-capacity `AudioStreamBuffer`; VL and future Unity integrations only
need to provide their host-specific audio-clock adapters.

Realtime consumers must use `WorkflowEventDetail.Outputs`.
`WorkflowEventDetail.Terminal` intentionally withholds intermediate chunks.
