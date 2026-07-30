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

`Nodetool.SDK.VL` is the managed adapter assembly and is not published as a
separate package. Install the user-facing `VL.Nodetool` package instead:

```text
nuget install VL.Nodetool -Version 0.1.6
```

## Usage (overview)

### vvvv gamma: how to reference this

In vvvv gamma, reference either:

- `Nodetool.SDK.VL.csproj` directly (good for development),
- the built DLLs directly, or
- `nuget install VL.Nodetool`.

DLLs:

- `nodetool-sdk\csharp\Nodetool.SDK.VL\bin\Release\net8.0\Nodetool.SDK.VL.dll`
- `nodetool-sdk\csharp\Nodetool.SDK.VL\bin\Release\net8.0\Nodetool.SDK.dll`
- `nodetool-sdk\csharp\Nodetool.SDK.VL\bin\Release\net8.0\Nodetool.Types.dll`

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
- Connection and workflow-catalog lifetimes are scoped to the active vvvv
  `AppHost`. Host disposal closes its portable connection session and catalog;
  live nodes resolve the service belonging to their current host rather than
  retaining a disposed service from an earlier patch/runtime instance.
- Connect-node endpoint/authentication, timeout, media threshold,
  auto-reconnect, and discovery-mode settings are also isolated per AppHost;
  separate runtime instances do not share mutable connection configuration.
- Individual-node and workflow discovery borrow the HTTP API client owned by
  that connection session. They share endpoint derivation, authentication,
  retry policy, and disposal with execution; factories do not dispose the
  borrowed client.
- The Connect node exposes hidden advanced defaults for execution persistence
  (`Job`/`Session`), event detail (`Full`/`Outputs`/`Terminal`), and output
  asset persistence (`Auto`/`Temporary`). `Temporary` is the SDK default:
  generated assets are not autosaved. Select `Auto` explicitly when outputs
  should enter the normal persistent asset library. This does not change the
  NodeTool web or Electron defaults. Values are submitted only after the
  server's capabilities advertise support, and apply to workflow and
  individual-node runs in that AppHost.
- For interactive low-overhead use, prefer `EventDetail = Outputs`. It
  suppresses ordinary node and edge events while preserving workflow outputs
  as they are emitted during the run.
- `EventDetail = Terminal` is final-result-only behavior, not a transparent
  performance switch. Outputs emitted during a workflow run are withheld from
  vvvv until the terminal result snapshot arrives. Use it only when delayed
  output delivery is acceptable.
- `EventDetail = Full` remains the default and provides complete execution
  visibility for diagnostics.

Workflow discovery transport:

- HTTP remains the default bootstrap transport, so loading the package does not require an open execution socket.
- Enable `UseWebSocketDiscovery` on the Connect node to switch workflow summaries and interface batches to correlated MessagePack RPC after the shared socket connects.
- `LoadNodes` and `LoadWorkflows` default to enabled. Disable either catalog to
  keep its generated entries out of the vvvv node browser; the Connect and
  diagnostic nodes remain available.
- Pulse `RefreshDiscovery` to refresh both enabled catalogs and republish
  changed node descriptions without restarting vvvv.
- When the socket connects, both enabled catalogs request a refresh regardless
  of whether workflow discovery uses HTTP or WebSocket. A later disconnect
  keeps the last successful workflow nodes available.
- Connection changes retain the last workflow factory while the replacement is fetched. A single empty discovery response is treated as provisional and must be confirmed before existing workflow nodes are removed.

Media transport:

- `InlineMediaLimitBytes` defaults to 10 MiB. With the default `AssetPersistence = Temporary`, larger local media uses the advertised SDK temporary-upload route and is represented by a storage URI. This skips persistent asset metadata and thumbnail work. Selecting `Auto`, or connecting to a server without that profile, uses the normal asset API and `asset_id`.
- Set the limit to `0` to upload all binary media, or raise it up to 64 MiB when inline transport is preferable.
- Workflow and individual-node inputs for file-backed audio, video, document, generic asset, folder, font, and model values use vvvv's native `Path` type (and `Spread<Path>` for lists). The SDK reads or uploads those files and constructs the NodeTool asset payload only while preparing execution.
- Use `Nodetool.Assets -> UploadAsset` when a file should be uploaded once
  and reused as an asset reference across runs. `Temporary` defaults to true
  and avoids database/thumbnail work; disable it only to add the upload to the
  persistent NodeTool asset library. MIME type is inferred from the extension,
  with a hidden `ContentType` override for uncommon formats.
- Media outputs remain typed SDK asset references (`AudioRef`, `VideoRef`, `DocumentRef`, `GenericAssetRef`, and the other specialized reference types), preserving URI, IDs, inline data, metadata, and media-specific properties.
- Individual NodeTool image and text-reference pins remain typed references where the node metadata identifies those types. Namespaced `type_name` values retain their native shape.
- Image pins use `SKImage`. A workflow node owns images it produces and disposes them when replaced or when the node is disposed; downstream patches should treat output images as borrowed and must not dispose them. Input images remain owned by the caller.
- NodeTool list metadata maps to immutable VL-native `Spread<T>` pins. Spreads are converted to transport arrays only while preparing execution parameters.
- Image outputs accept inline bytes, data URIs, HTTP/storage URLs, and current `asset://<stored-file>` references. ID-only asset references are materialized through the connected asset RPC.
- Workflow outputs are latched and reapplied on each VL update so event-driven scalar and media results remain visible after the execution frame.
- Structured workflow pins use generated `Nodetool.Types` DTOs when the interface discriminator or `type_name` resolves in the SDK registry. Unknown structures remain explicit `Object` fallback pins.

### Realtime audio outputs

Workflow outputs that the server declares with `stream_kind: "audio"` get a
visible `<Output Name> Audio Source` companion pin. Connect it to VL.Audio's
`AudioSourceToAudioSignal` node. The ordinary workflow output remains unchanged
and continues to carry its normal accumulated or final value. Connect one
`AudioSourceToAudioSignal` consumer, then branch its audio-signal outputs when
multiple downstream consumers are needed.

The adapter is not generated for text, control, unspecified, or future stream
kinds. This keeps audio pins authoritative and avoids treating every generic
`chunk` output as PCM audio.

The current adapter plays raw `pcm16le`/`pcm` and `f32le` chunks. Semantic
audio streams using MP3, Opus, A-law, or mu-law remain valid ordinary workflow
outputs but are not decoded by the companion Audio Source. Select a PCM output
format on configurable realtime-audio nodes when direct VL.Audio playback is
required. An unsupported encoding is reported once without failing the
workflow itself.

The adapter implements the `IAudioSource` contract already provided by
`VL.Core`; `Nodetool.SDK.VL` therefore does not add a hard dependency on
VL.Audio or change its portable `net8.0` target. The first audio chunk sets the
native sample rate and channel count. Playback converts to the audio engine's
requested rate and channel layout, returns silence on underrun, drops newest
frames when its fixed four-second buffer is full, and resets at the start of
each workflow run. Cancellation, reconnect gaps, and completion drain any
already buffered samples and then produce silence.

Use `EventDetail = Outputs` on the Connect node. `Terminal` deliberately
withholds intermediate chunks and cannot drive realtime playback. The steady
audio callback reuses its frame storage and does not lock or allocate; a
one-time allocation occurs when the callback block shape changes.

### Workflow nodes

Generated from the authoritative workflow-interface v1 contract, over compact HTTP by default or correlated WebSocket RPC when enabled on the Connect node.

Generated workflow and individual-node execution inputs are consistently
named `Run`. The pin fires on a rising edge; `AutoRun` remains the separate
input-change mode. Patches created against the former `Trigger` or `Execute`
pin names must reconnect that control link once.

Category:

- `Nodetool Workflows`

Discovery diagnostics:

- `Nodetool Workflows -> WorkflowAPIStatus` reports the last successful UTC refresh, server version, authoritative interface source, workflow count, and last discovery error.
- Pulse `Refresh` to request another compact discovery pass. Unchanged interfaces are reused by workflow revision, node-registry revision, and interface etag.
- If a refresh fails after a successful load, the existing workflow nodes stay available and `Status` reports that their metadata is stale.

vvvv logging under the `VL.Nodetool` category is intentionally restrained. A healthy AppHost emits one
summary after connection-backed discovery and both dynamic factories have
resolved. Actionable errors remain visible and repeated identical discovery
errors are suppressed until recovery or reset. Set
`NODETOOL_VL_VERBOSE=1` to include assembly, factory, execution-timing, pin,
and routine refresh details while diagnosing a problem.

### Node nodes

Generated from `GET /api/nodes/metadata`.

Category:

- `Nodetool Nodes.*`

### Image helpers

- `Nodetool -> DecodeImageRef`
- `Nodetool.Images -> DecodeImageRefToSKImage`

### Asset helpers

- `Nodetool.Assets -> AssetAsFile`
- `Nodetool.Assets -> UploadAsset`
- `Nodetool.Assets -> SaveAsset`

`AssetAsFile` accepts a typed NodeTool asset reference and asynchronously
produces a native vvvv `Path`. Existing local files pass through unchanged.
Inline bytes and data URIs are written to a deterministic cache; HTTP,
`/api/storage`, `asset://`, and ID-only references are resolved and downloaded.
The node reports loading, ready, cache-hit, content-type, source-URI, and error
state. Pulse `Refresh` to replace an existing cached copy.

The default cache is:

```text
%LOCALAPPDATA%\Nodetool\SdkCache\assets
```

The helper is intended for file-backed image, audio, video, document, text,
font, model, and generic assets. Folder references do not materialize as a
single file.

`UploadAsset` explicitly uploads a local `Path` and returns a reusable typed
asset reference. It does not replace ordinary workflow media pins: file-backed
audio/video/document/model inputs can still receive `Path` directly, while
computed workflow images remain `SKImage`. Use a normal vvvv image loader when
an image file should feed an `SKImage` pin.

`SaveAsset` materializes an asset through the same cache and atomically copies
it to a selected file or directory. It never persists the asset on the
NodeTool server. Missing filename extensions are copied from the materialized
source; directory destinations use the asset's reported name. Existing files
are protected unless `Overwrite` is enabled.
