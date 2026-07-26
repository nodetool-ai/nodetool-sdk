# Nodetool.SDK

.NET 8 client library for discovering and executing NodeTool workflows and
nodes over the current HTTP and MessagePack WebSocket APIs.

## Installation

```xml
<PackageReference Include="Nodetool.SDK" Version="0.1.6" />
```

Workflow-only applications need only `Nodetool.SDK`. The generated
`Nodetool.Types` catalog is optional and is not loaded by the portable base.
Host adapters that expose generated structured pins can reference the catalog
and pass its assembly to `NodeToolTypeRegistry.RegisterAllTypes(...)`.

## Recommended connection entry point

Use one `NodeToolConnectionManager` per host connection scope. It derives the
matching HTTP and WebSocket endpoints, owns both clients, applies bearer
authentication to HTTP requests and the WebSocket upgrade, and implements
`INodeToolExecutionConnection` for `WorkflowExecutionRuntime`:

```csharp
using Nodetool.SDK.Connection;

var profile = new NodeToolConnectionProfile
{
    ServerUrl = new Uri("https://cloud.example/nodetool"),
    TokenProvider = new StaticNodeToolTokenProvider(
        Environment.GetEnvironmentVariable("NODETOOL_TOKEN"))
};

await using var connection = new NodeToolConnectionManager(profile);
var api = await connection.GetApiClientAsync();
var execution = await connection.GetConnectedClientAsync();

var capabilities = await api.GetSdkCapabilitiesAsync();
var workflows = await execution.GetWorkflowSummariesAsync();
```

Hosts with expiring credentials can implement `INodeToolTokenProvider`; it is
evaluated again for each WebSocket connection attempt. Explicit
`ApiBaseUrl`/`WorkerWebSocketUrl` overrides remain available for deployments
whose endpoints do not share one origin. An injected `HttpClient` remains
owned by the caller. Manager-created HTTP clients use a bounded retry policy
only for side-effect-free SDK discovery and preflight calls. Correlated
WebSocket discovery reads use the same bounded policy for transient transport
failures. Every HTTP attempt retains one `X-NodeTool-Request-Id`; every
WebSocket attempt retains one logical `request_id`; and HTTP `Retry-After` is
respected within the configured maximum delay. Workflow submission and
cancellation are never retried. Direct `NodetoolClient` and
`NodeToolExecutionClient` construction keep single-attempt behavior unless a
`NodeToolReadRetryPolicy` is supplied.

Hosts whose connection settings can change at runtime can own a
`NodeToolConnectionSession`. It replaces and disposes the underlying manager
when its immutable profile changes, rejects stale in-flight connections,
projects connection status/errors, and supports reconnect or reset without
host-specific lifecycle code. The VL adapter uses this portable session;
future C# hosts can use the same boundary.

Caller-owned `HttpClient` instances retain their base address, timeout, and
default headers. NodeTool endpoint, authentication, and user-agent headers are
applied per request, including deployments hosted below a reverse-proxy
subpath.

`NodeToolExecutionClient` also accepts an optional
`INodeToolWebSocketTransport`. This is primarily a host-testing seam: normal
applications should use the built-in MessagePack transport, while adapters can
test connection, token refresh, run, cancellation, failure, and reconnect
behavior without opening a socket.

## WebSocket execution

```csharp
using Nodetool.SDK.Configuration;
using Nodetool.SDK.Execution;

var options = new NodeToolClientOptions
{
    WorkerWebSocketUrl = new Uri("ws://127.0.0.1:7777/ws"),
    ApiBaseUrl = new Uri("http://127.0.0.1:7777"),
    AutoReconnect = true
};

await using var client = new NodeToolExecutionClient(options);
if (!await client.ConnectAsync())
    throw new InvalidOperationException(client.LastError ?? "Connection failed.");

var session = await client.ExecuteWorkflowAsync(
    "workflow-id",
    new Dictionary<string, object>
    {
        ["prompt"] = "hello from C#"
    });

session.OutputReceived += update =>
{
    Console.WriteLine(
        $"{update.NodeName}.{update.OutputName}: {update.Value.ToJsonString()}");
};

var succeeded = await session.WaitForCompletionAsync();
if (!succeeded)
    Console.WriteLine(session.ErrorMessage);
```

`WaitForCompletionAsync` returns `false` when the remote job fails or is
cancelled. Cancelling its token cancels only the local wait and throws
`OperationCanceledException`. Call `session.CancelAsync()` to cancel the remote
job.

For latency-sensitive clients, explicit execution options can skip durable job
history, ordinary node/edge events, or generated-asset autosave:

```csharp
var executionOptions = new WorkflowExecutionOptions(
    WorkflowPersistence.Session,
    WorkflowEventDetail.Outputs,
    WorkflowAssetPersistence.Temporary);

var session = await client.ExecuteWorkflowAsync(
    "workflow-id",
    inputs,
    executionOptions);
```

Omitting the options preserves normal server behavior. Session persistence
does not provide completed-job history or reconnect after the server loses the
in-memory run. Temporary assets are suitable only while their returned
references remain valid; use automatic asset persistence when outputs must be
durable.

Connection sessions cache the server capability document for their current
profile, so repeated negotiated runs do not add one HTTP discovery request per
execution. Replacing or resetting the profile creates a fresh capability
scope.

The execution client supports:

- workflow execution by ID or case-insensitive name;
- single-node and explicit-graph execution;
- output, preview, node, progress, and completion events;
- queued and running job cancellation;
- reconnecting active sessions;
- compact workflow-summary and authoritative workflow-interface discovery;
- full node metadata and recursive node-type inventory discovery.

For host integrations, `WorkflowExecutionController` wraps the same
job-scoped session with immutable snapshots, public workflow-output routing,
stream accumulation, terminal reconciliation, timeout, cancellation, retained
outputs, and coalesced rerun policies:

```csharp
var workflow = catalog.GetSnapshot().Workflows
    .Single(item => item.Id == "workflow-id");
await using var controller = WorkflowExecutionControllerFactory.Create(
    client,
    workflow,
    options.ApiBaseUrl,
    options.AuthToken);

controller.SnapshotChanged += snapshot =>
{
    Console.WriteLine($"{snapshot.State}: {snapshot.Progress:P0}");
};

await controller.StartAsync(new WorkflowInvocation(
    workflow.Id,
    new Dictionary<string, object?>
    {
        ["prompt"] = "hello from a host adapter"
    },
    Timeout: TimeSpan.FromMinutes(2)));
await controller.WaitForTerminalAsync();
```

`StartAsync` rejects overlapping runs. Host adapters that react to changing
inputs can instead call `RequestStartAsync` with `QueueLatest` or
`CancelAndRestart`; both policies coalesce pending requests and execute only
the latest invocation. Outputs remain available while the next run starts by
default. Set `RetainOutputs: false` on an invocation when clearing them is
preferred.

Snapshot callbacks run on protocol/background threads. UI and engine hosts
must marshal them onto their own frame or main thread.

Frame-based adapters can reuse `OnInputChangeScheduler` for auto-run edge
policy and `WorkflowOutputUpdateTracker` to apply only newer output snapshots.
`NodeToolValuePresentation` handles typed strings and streamed chunk/list
shapes without depending on a UI framework.

Higher-level hosts can implement `INodeToolExecutionConnection` and use
`WorkflowExecutionRuntime` instead of coordinating these pieces themselves.
The runtime obtains a connected shared client without taking ownership,
prepares recursive media inputs, keeps one end-to-end timeout budget, replaces
its controller when reconnect yields a new client, exposes immutable
snapshots, and owns run cancellation/disposal. Its result includes connection,
input-preparation, remote-execution, and total timings. Unity and VL adapters
therefore only need to supply native-media conversion and main-thread
projection.

Before connecting, the runtime performs conservative validation against the
discovered workflow interface. Obvious missing/null required inputs and
numeric bound violations fail locally; unknown inputs are reported as
non-blocking warnings for forward compatibility. Server preflight remains the
authoritative validation and availability check.

## HTTP API

Use the HTTP client for health checks, metadata, workflow interfaces,
preflight, assets, and job inspection:

```csharp
using Nodetool.SDK.Api;
using Nodetool.SDK.Api.Models;

using var client = new NodetoolClient(new Uri("http://127.0.0.1:7777"));

var health = await client.GetHealthAsync();
var summaries = await client.GetWorkflowSummariesAsync();
var interfaces = await client.GetWorkflowInterfacesAsync(
    summaries.Take(20).Select(workflow => workflow.Id).ToArray());
var nodeTypes = await client.GetNodeTypeInventoryAsync(cursor: 0, limit: 100);

var preflight = await client.PreflightWorkflowAsync(new SdkPreflightRequest
{
    WorkflowId = summaries[0].Id,
    Level = SdkPreflightLevels.Availability,
    Inputs = new Dictionary<string, object?>()
});
```

Execution preflight can optionally select the intended target:

```csharp
var remotePreflight = await client.PreflightWorkflowAsync(
    new SdkPreflightRequest
    {
        WorkflowId = summaries[0].Id,
        Level = SdkPreflightLevels.Execution,
        ExecutionTarget = new SdkExecutionTarget
        {
            Kind = SdkExecutionTargetKinds.Worker,
            WorkerId = "worker-id"
        }
    });
```

Omitting `ExecutionTarget` preserves local/default behavior. A worker selector
never falls back to another attached worker. Until a live WebSocket runner
identity is available, current queue capacity is reported as non-blocking
unknown rather than inferred from an unrelated connection.

After a supporting execution client connects, its
`INodeToolExecutionClient.ExecutionTargetId` is populated asynchronously from
the server announcement. Passing that value as a `runner` target requests the
capacity snapshot for that exact authenticated connection:

```csharp
var runnerId = executionClient.ExecutionTargetId;
if (runnerId is not null)
{
    var request = new SdkPreflightRequest
    {
        WorkflowId = workflow.Id,
        Level = SdkPreflightLevels.Execution,
        ExecutionTarget = new SdkExecutionTarget
        {
            Kind = SdkExecutionTargetKinds.Runner,
            RunnerId = runnerId
        }
    };
    var exactRunnerPreflight =
        await apiClient.PreflightWorkflowAsync(request);
}
```

`NodetoolClient(Uri, ...)` is the preferred constructor because the endpoint is
explicit. The parameterless constructor retains the localhost development
endpoint for convenience.

Execution is intentionally not exposed on `NodetoolClient`: the current server
executes workflows, graphs, and individual nodes through
`INodeToolExecutionClient` over the job-scoped WebSocket protocol. This avoids
advertising historical synchronous HTTP routes that the server does not
implement.

When a caller injects an `HttpClient`, disposing `NodetoolClient` does not
dispose that externally owned instance.

## Assets

The asset manager uses the canonical typed references from
`Nodetool.SDK.Types.Assets`:

```csharp
using Nodetool.SDK.Api;
using Nodetool.SDK.Assets;
using Nodetool.SDK.Types.Assets;

using var api = new NodetoolClient(new Uri("http://127.0.0.1:7777"));
using var assets = new AssetManager(nodetoolClient: api);

AssetRef uploaded = await assets.UploadAssetAsync(
    @"C:\media\sample.wav",
    "audio/wav");

await using var stream = File.OpenRead(@"C:\media\large-video.mp4");
AssetRef streamed = await assets.UploadAssetAsync(
    "large-video.mp4",
    stream,
    "video/mp4");

var audio = (AudioRef)uploaded;
Console.WriteLine(audio.Uri);

var materializer = new AssetMaterializer(
    resolveAsset: api.GetAssetAsync,
    apiBaseUrl: new Uri("http://127.0.0.1:7777"),
    cacheDirectory: @"C:\temp\nodetool-assets");
var file = await materializer.MaterializeAsync(audio);
Console.WriteLine(file.Path);
```

Uploads preserve their MIME content type and return `ImageRef`, `AudioRef`,
`VideoRef`, `DocumentRef`, or `GenericAssetRef` as appropriate. Downloads
support HTTP(S), data URIs, file URIs, and existing local paths. Stream uploads
do not take ownership of the caller's stream.

`IAssetMaterializer` is the host-neutral file boundary used by the VL adapter
and intended for other C# hosts. It supports local files, inline bytes/text,
data URIs, HTTP(S), storage references, and ID-only NodeTool assets. Remote
content is published atomically into an identity-addressed cache. Connection
bearer tokens are attached only to same-origin downloads.

`MediaInputPreparer` is the corresponding execution-input boundary. It accepts
asset references, local paths, URIs, and byte buffers; inlines small local
values and uploads values above a configurable size through an injected
`IAssetManager`. Engine-specific image objects should be encoded to bytes by
the host adapter before calling it. Common image, audio, video, document,
glTF/GLB model, and font extensions receive specialized MIME types; common
binary image/audio/video/document/model signatures are detected when no
filename is available.

`WorkflowInputPreparer` applies that behavior recursively from a
`WorkflowDescriptor`, including list/array media pins, while using the
portable value converter for ordinary and forward-compatible unknown inputs.
`WorkflowInputPreparationService` adds connection-scoped asset upload policy
and creates HTTP clients only when the descriptor contains media inputs.
Hosts may inject one media-value adapter—for example, Unity texture encoding
or VL `SKImage` encoding—without reimplementing file and upload policy.

`NodeToolAssetValueParser` converts output payloads into canonical typed asset
references without I/O. `AssetReferenceUri` recognizes NodeTool storage
references, while `IAssetMaterializer` remains the download/cache boundary.

The default asset cache is:

```text
%USERPROFILE%\.nodetool\cache\assets
```

An injected `HttpClient` remains owned by the caller. An internally created
client is disposed with `AssetManager`.

## NodeTool type mapping

| NodeTool type | C# representation |
| --- | --- |
| `str`, `text` | `string` |
| `int` | `int` |
| `float` | `float` |
| `bool` | `bool` |
| `list` | typed collection or `List<object>` |
| `dict`, `object` | `Dictionary<string, object>` or generated DTO |
| `image` | `ImageRef` |
| `audio` | `AudioRef` |
| `video` | `VideoRef` |
| `document` | `DocumentRef` |
| `asset` | `GenericAssetRef` |

Dynamic values received during execution are exposed as `NodeToolValue`, which
preserves scalar, binary, list, map, and typed-reference shapes without forcing
callers through JSON strings.

The same `WorkflowExecutionOptions` accepted by workflow runs can be supplied
to single-node runs. Hosts should negotiate non-default persistence, event
detail, and asset-persistence values against SDK capabilities before
submission; default options are omitted from the wire.

## Test console

```bash
cd /m/P/NODETOOL/____REPOS____/nodetool-sdk/csharp/Nodetool.SDK
dotnet run -c Release --project ./TestConsole/Nodetool.SDK.TestConsole.csproj -- \
  run-workflow \
  --ws ws://127.0.0.1:7777/ws \
  --workflow "SDK Test - Primitives" \
  --input str=hello \
  --input int=3 \
  --input float=0.5 \
  --input bool=false \
  --input select=fast \
  --input 'string list=["alpha","beta"]'
```

Repeated `--input name=value` arguments accept JSON scalars, arrays, and
objects; unquoted values remain strings. Supply any required media input for
the selected workflow as an asset reference or URI.

## Build and package verification

From `csharp/`:

```powershell
.\regen-and-verify.ps1 `
  -SkipGeneration `
  -SkipGitDiff `
  -NoRestore `
  -IncludeVL `
  -IncludeVLTests `
  -VerifySdkPackage `
  -VerifyVLPackage
```

`-VerifySdkPackage` verifies that the portable package neither depends on nor
contains `Nodetool.Types`. Use `-NoRestore` only after restoring the projects;
any failed build, test, or pack command terminates verification.

## VL/vvvv

Use the separate `Nodetool.SDK.VL` / `VL.Nodetool` package for vvvv gamma
integration, dynamic node factories, native `Path` media inputs, and
`Spread<T>` collections.

## License

AGPL-3.0-only.
