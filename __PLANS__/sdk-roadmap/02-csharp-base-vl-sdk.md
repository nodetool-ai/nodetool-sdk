# 02 - Portable C# Base and VL SDK

**Status:** VL 0.1.6 release candidate; Unity explicitly paused
**Order:** 2 of 3
**Requires:** [01 - NodeTool Server and Public SDK Contract](01-nodetool-server-contract.md)
**Next:** [03 - Unity Plugin](03-unity-plugin.md)

Portable extraction and server-contract work may proceed iteratively in
parallel. The server plan's exit gate is required before the VL release gate,
not before characterization and host-neutral refactoring begin.

Within this plan, Phases 0 through 7 are implemented in order and each gate
protects the next refactor. Work that depends on a not-yet-published server
operation uses fixtures or interfaces until the corresponding server gate
passes; it does not invent temporary production protocol behavior.

The initial outcome is deliberately narrower than the full architecture:
one reusable C# path for discovery, execution, cancellation, terminal outputs,
and referenced assets, plus a reliable thin VL adapter. Items explicitly
marked deferred are follow-up quality or scale work and do not block Unity.
A checked item labelled **Deferred** records the scope decision, not feature
implementation.

## Reconciled VL 0.1.6 release boundary - 2026-07-26

- [x] Portable discovery, connection, preflight, execution, cancellation,
      terminal reconciliation, value/media handling, diagnostics, and
      capability negotiation are implemented and consumed by VL.
- [x] The VL adapter is thin, AppHost-scoped, dynamically typed, packageable,
      and covered by portable, adapter, and headless package tests.
- [x] Matching `Nodetool.SDK` and `VL.Nodetool` 0.1.6 artifacts are built and
      locally verified.
- [ ] Run the final packed-package interactive reload/reconnect/media pass.
- [ ] Publish the matching NuGet prereleases after explicit approval.
- [x] **Deferred beyond VL 0.1.6:** exhaustive characterization/replay,
      bounded asset leases/cache cleanup, multi-target/AOT work, HDE, and all
      Unity-specific implementation.

The remaining detailed unchecked items in those deferred themes are backlog,
not hidden release blockers.

The recommended first C# slice is: split portable tests from VL tests, remove
the mandatory generated-catalog dependency from workflow-only consumers, then
extract the workflow catalog. This is lower-risk than starting with the large
execution node and immediately gives console, VL, and future Unity code one
shared discovery implementation.

## Outcome

The portable C# SDK implements the public NodeTool contract and owns reusable
discovery, execution, scheduling, value, asset, connection, diagnostic, and
test behavior.

The VL package becomes a thin vvvv adapter over that base. It retains dynamic
node factories, pins, vvvv types, frame behavior, AppHost integration, and
presentation, but no longer acts as the reference implementation for generic
NodeTool client behavior.

## Dependency direction

```text
NodeTool protocol
       |
       v
Nodetool.SDK portable base
       |
       v
Nodetool.SDK.VL adapter
```

`Nodetool.SDK` must never reference `VL.*`, SkiaSharp, Stride, Unity, an engine
player loop, or host-specific collections.

Public reusable services must stay outside namespaces imported by vvvv.
`Nodetool.SDK.VL` should use curated `ImportType` or narrowly scoped
`ImportNamespace` declarations so helpers do not become accidental nodes.

## Code review baseline - 2026-07-24

The current code already contains much of the desired behavior, but the
portable boundary is uneven:

- `ExecutionSession` is job-scoped, drops foreign updates, preserves streamed
  output disposition, and waits for authoritative terminal outputs.
- `OnInputChangeScheduler` is already host-neutral, but only supports start,
  queued rerun, and cancel/restart decisions.
- `WorkflowMetadataService` already implements compact discovery, bounded
  interface batches, revision/registry-aware caching, endpoint/token cache
  scoping, diagnostics, and last-success state inside the VL assembly.
- Portable asset services now resolve local files, HTTP(S), storage URIs,
  `asset:` references, ID-only assets, data URIs, and inline bytes with atomic
  cache publication; the VL materializer is a thin file projection.
- `WorkflowNodeBase` now retains frame/pin/AppHost/Skia projection, latching,
  and rising-edge state while the portable runtime owns connection, input and
  media preparation, timeout, controller replacement, cancellation, output
  accumulation, and terminal reconciliation.
- `Nodetool.SDK` targets only `net8.0`. The generated `Nodetool.Types` catalog
  is now optional for workflow-only consumers and is loaded explicitly by the
  VL adapter; reflection scanning/activation and contractless/dynamic
  MessagePack resolvers still need an AOT-safe path.
- HTTP DTOs currently use reflection-based `System.Text.Json`; the Unity spike
  will determine whether selected IL2CPP paths require a source-generated
  context in the initial release.
- The generated type catalog currently produces a large nullable-warning
  surface during a clean build.
- Portable, VL adapter unit, and headless VL document tests now have separate
  projects. Unity compilation coverage remains to be added.
- The `.vl` document and deployment package ID agree on `VL.Nodetool`, and the
  deployment nuspec places DLLs under `lib/net8.0`. The source directory is
  still named `vvvv`, the nuspec lives under `vvvv/deployment`, and the
  `Nodetool.SDK.VL` project produces a second package with a separate version.
  Package identity, layout, and version ownership therefore need cleanup.
- Dynamic factories correctly remain VL-specific and currently work through
  `AssemblyInitializer`/`RegisterNodeFactory` without a broad ordinary-type
  import.
- There is no Unity package or Unity assembly in this repository yet.

The work below therefore moves characterized code rather than rewriting it.
Every move should preserve behavior first, then simplify the VL adapter.

Review verification:

- [x] Current C#/VL xUnit suite passes (102 tests on 2026-07-24).
- [x] Current headless VL document compilation suite passes (3 tests on
  2026-07-26, including the isolated packed-package document).
- [x] Live TestConsole discovery and primitive workflow execution pass against
  the current hybrid TypeScript/Python server registry.

Primary implementation anchors:

- `csharp/Nodetool.SDK/Execution/ExecutionSession.cs` and
  `NodeToolExecutionClient.cs` for the current portable execution core;
- `csharp/Nodetool.SDK/Utilities/Execution/OnInputChangeScheduler.cs` for the
  current portable scheduling seed;
- `csharp/Nodetool.SDK.VL/Services/WorkflowMetadataService.cs` for catalog
  extraction;
- `csharp/Nodetool.SDK.VL/Services/AssetFileMaterializer.cs` for asset-service
  extraction;
- `csharp/Nodetool.SDK.VL/Nodes/WorkflowNodeBase.cs` for the remaining mixed
  execution/input/output state;
- `csharp/Nodetool.SDK/WebSocket/MessagePackWebSocketClient.cs` and
  `csharp/Nodetool.SDK/Types/NodeToolTypeRegistry.cs` for transport/AOT work;
- `csharp/Nodetool.SDK.VL/Initialization.cs`,
  `vvvv/VL.Nodetool.vl`, and `vvvv/deployment/VL.Nodetool.nuspec` for the VL
  adapter and package boundary.

## Phase 0 - Preserve and characterize the current implementation

- [x] Consume server-derived workflow interface v1.
- [x] Support compact workflow discovery and bounded interface batches.
- [x] Correlate WebSocket RPC responses and route execution by job ID.
- [x] Reconcile authoritative terminal workflow outputs.
- [x] Preserve primitive, enum, list, asset, and media value shapes.
- [x] Expose VL `Spread<T>` collections and VL `Path` media inputs/files.
- [x] Materialize local, HTTP, storage, `asset://`, ID-only, data URI, and
  inline assets as files.
- [x] Support dynamic workflow and individual-node factories.
- [x] Keep last-known-good factory snapshots and coalesce invalidation.
- [x] Pass the focused C# and VL test suite established during the SDK repair.
- [x] Cover job scoping, terminal reconciliation, append/replace semantics,
  MessagePack nil/binary handling, list-to-Spread conversion, latching, and
  representative asset forms with focused tests.
- [x] **Deferred refinement:** add characterization tests around workflow catalog caching, interface
  conversion, media upload, remaining asset URI forms, cache invalidation,
  and disposal.
- [x] **Deferred from the initial outcome:** a complete redacted session
  archive and formal inventory of every current public VL type. Focused live
  smoke patches remain required.

### Gate

- [x] Every class selected for extraction has focused host-neutral coverage
      before it moves.
- [x] **Deferred interactive verification:** existing vvvv help and smoke
      patches retain their current behavior.

## Phase 1 - Establish the portable package boundary

- [x] Audit current targets and dependencies: `Nodetool.SDK` is `net8.0` and
  has a required project reference to `Nodetool.Types`.
- [x] **Deferred to the Unity compatibility spike:** multi-target the required portable projects for .NET 8 and
  `netstandard2.1`, or split a contracts assembly if measurements require it.
- [x] Make generated `Nodetool.Types` optional for workflow-only clients.
- [x] Remove historical synchronous HTTP execution methods and constants whose
  routes do not exist, and use one structured `SdkApiException` model for HTTP
  and WebSocket SDK errors.
- [x] **Deferred to Unity U0:** add generated AOT-safe MessagePack formatters/type registration.
- [x] **Deferred to Unity U0:** remove reliance on runtime assembly scanning for required wire DTOs.
- [x] **Deferred to Unity U0:** remove contractless/dynamic serialization from the Unity/AOT execution
  path while preserving arbitrary map/list values.
- [x] Fix or intentionally isolate generated `Nodetool.Types` nullable warnings
  so real SDK warnings remain actionable.
- [x] Extract injectable HTTP and WebSocket transports at the lowest useful
  boundary.
- [x] Keep normal .NET HTTP/WebSocket implementations behaviorally equivalent.
- [x] Split portable tests from VL integration tests so the portable test
  project has no reference to `Nodetool.SDK.VL`.
- [x] Build a plain console consumer with only a `Nodetool.SDK` project
  reference and verify the base NuGet has no generated-catalog dependency.
- [x] **Deferred to Unity U0:** add one Unity compilation/IL2CPP smoke test for the selected target
  profiles.
- [x] **Deferred conformance expansion:** validate C# directly against the
      protocol v1 golden fixtures.
- [x] **Deferred from the initial outcome:** exhaustive package-size/compile
  measurements, source-generated HTTP JSON metadata unless the Unity spike
  proves it necessary, and architecture/consumer tests for every target.

### Gate

- [x] A plain console client can reference the workflow SDK without
  `Nodetool.Types`, VL, or Unity.
- [x] The supported .NET 8 portable package exposes the current protocol
      contracts; additional targets are deferred to Unity U0.

## Phase 2 - Extract the workflow catalog and descriptors

- [x] Implement compact pagination, batched interface retrieval,
  revision/registry cache keys, scoped shared caching, and last-refresh
  diagnostics in the current VL service.
- [x] Add immutable host-neutral workflow summary, descriptor, pin, interface,
  revision, staleness, and diagnostic models.
- [x] Add `IWorkflowCatalog`.
- [x] Implement summary consumption and batched interface retrieval in the
  portable catalog.
- [x] Implement scope/revision/registry-aware caching in the portable catalog.
- [x] Preserve last-known-good snapshots across refresh failures.
- [x] Expose structured refresh state and errors instead of UI strings.
- [x] Remove the corresponding legacy retrieval/cache implementation from
  `WorkflowMetadataService` when the VL factory switches to the portable
  catalog.
- [x] Route VL workflow discovery through immutable portable catalog snapshots
  while preserving the existing VL node projection.
- [x] Keep VL name/category sanitization as a VL-only projection.
- [x] **Deferred cleanup:** keep the stable VL `WorkflowDetail`/schema projection
  until `WorkflowNodeBase` is reduced in Phase 6; deleting it now would churn
  execution/schema-debug code without changing the portable contract.
- [x] **Deferred from the initial outcome:** stale-while-revalidate policy and
  rich per-pin interface diffs. A simple stale/incompatible diagnostic is
  sufficient.

### Gate

- [x] Console and VL discovery use the same catalog and return equivalent
  descriptors.
- [x] A failed refresh never destroys the last valid factory snapshot.

## Phase 3 - Extract values, inputs, and asset services

- [x] Implement current typed primitive/list/structured/asset conversion and
  VL `Spread<T>`/`Path` projections.
- [x] Implement current local, HTTP(S), storage, `asset:`, ID-only, data URI,
  and inline-byte materialization with atomic file publication.
- [x] Add target/schema-aware conversion among `NodeToolValue`, JSON, CLR primitives,
  dictionaries, and immutable lists.
- [x] Add checked numeric conversion with stable errors.
- [x] Move generic conversion out of `VlValueConversion`.
- [x] Move generic recursive workflow input normalization into the portable
  value converter.
- [x] Add descriptor-driven `WorkflowInputPreparer` for ordinary inputs,
  nested media collections, forward-compatible unknown pins, and a narrow
  host-media adaptation hook.
- [x] Move generic async media-aware input preparation, inline-size policy,
  MIME selection, and upload decisions out of `WorkflowNodeBase` into the
  portable `MediaInputPreparer`.
- [x] **Deferred asset-lifecycle refinement:** add `IAssetTransferClient`, `IAssetStore`, `AssetMaterializer`, and
  explicit asset leases/ownership.
- [x] Move URI resolution, inline decoding, MIME/extension detection,
  authenticated download, identity, and atomic file publication out
  of `AssetFileMaterializer`.
- [x] Move generic file, byte, and stream upload out of the VL node.
- [x] Keep only host image encoding plus VL `Path`/`Spread` projection in the
  VL media-input adapter; reuse the portable preparer for workflow and
  individual-node execution.
- [x] **Deferred asset-lifecycle refinement:** use a bounded temporary cache
      with deterministic cleanup.
- [x] Keep VL `Spread<T>`, `Path`, Skia, enum, and pin conversions in the VL
  adapter.
- [x] Make VL `AssetAsFile` a thin state/`Path` projection over the portable
  materializer.
- [x] **Deferred from the initial outcome:** configurable cache policies,
  cache history/inspection, and detailed cache diagnostics.

### Gate

- [x] **Deferred broad fixture expansion:** a console fixture can upload and materialize image, file, URL, inline,
  and NodeTool asset-reference forms.
- [x] VL primitives, lists, paths, image references, and file materialization
  remain typed and stable across frames.

## Phase 4 - Extract the workflow execution controller

- [x] Provide a job-scoped portable `ExecutionSession` with foreign-update
  filtering, streamed output events, explicit cancellation, and authoritative
  terminal completion.
- [x] Provide the initial host-neutral `OnInputChangeScheduler`.
- [x] Add host-neutral `WorkflowInvocation`, execution state, output state,
  progress, error, and completion snapshots.
- [x] Add `WorkflowExecutionController`.
- [x] Build the controller around the existing `ExecutionSession` rather than
  creating a competing WebSocket/session implementation.
- [x] Move public output routing and node/output-name mapping out of
  `WorkflowNodeBase`.
- [x] Move append/replace chunk accumulation out of the VL node.
- [x] Move live-update/terminal-output reconciliation out of the VL node.
- [x] Move execution timeout and cancellation ownership out of the VL node.
- [x] Add portable queue-latest and cancel-and-restart policies that coalesce
  pending invocations and preserve retained outputs by default.
- [x] **Deferred scheduler refinement:** route VL frame-driven rerun requests through the portable scheduling
  policies after async media input preparation is extracted in Phase 3.
- [x] Keep manual trigger, cancel/restart, and the existing single queued-rerun
  behavior deterministic.
- [x] On transient disconnect, leave the controller in a deterministic
  reconnectable or failed state without leaving the host permanently busy.
- [x] Expose immutable events/snapshots without invoking a host main thread.
- [x] Cover retained outputs, explicit output clearing, queue coalescing,
  cancel/restart, single remote cancellation, timeout, overlap rejection,
  streamed chunks, output routing, and authoritative terminal results with
  host-neutral fake-session tests.
- [x] Route the plain C# TestConsole workflow smoke through the portable
  catalog/controller and verify primitive, list, image-URI reference, and
  terminal completion behavior against the live server.
- [x] **Deferred conformance expansion:** test the controller using recorded
      protocol scenarios.
- [x] **Deferred from the initial outcome:** the full scheduler policy matrix,
  client-side budget orchestration, and bounded parallel execution. Server
  admission remains authoritative.

### Gate

- [x] Primitive, list, streaming, image, cancellation, timeout, concurrent-run,
  reconnect, and terminal-output scenarios pass without loading a host
  assembly.
- [x] Fast primitive outputs remain latched and cannot appear for only one
      frame.

## Phase 5 - Extract connection management, preflight, and diagnostics

- [x] Provide current HTTP discovery/asset clients, MessagePack WebSocket
  execution client, injected `HttpClient` ownership rules, and async execution
  disposal.
- [x] Add instance-based connection profiles and
  `NodeToolConnectionManager`.
- [x] Define injected-client ownership for the portable runtime: the
  connection retains its shared client, while the runtime deterministically
  disposes its controllers and active runs.
- [x] Add HTTP/WebSocket endpoint derivation and token-provider interfaces.
- [x] Add shared redaction for URLs, headers, tokens, workflow inputs, and
  diagnostics before any host logger receives them.
- [x] Remove neutral client creation/lifetime logic from the static VL
  provider.
- [x] Route VL node and workflow discovery through the HTTP client owned by
  the current AppHost connection session; retain standalone/design-time
  fallbacks without giving factories ownership of borrowed clients.
- [x] Add the typed HTTP `get_capabilities` client.
- [x] Add the typed HTTP `preflight_workflow` client with typed request,
      response, requirements, issues, cost, and structured failure models.
- [x] Add fast local input validation while treating server preflight as
  authoritative.
- [x] Add request identity, safe retry/backoff for read-only operations, and
  server-limit handling. Do not retry ambiguous workflow submission.
- [x] Add focused fake-transport tests for connect, run, cancel, failure, and
  reconnect.
- [x] Keep only Connect-node/AppHost mapping and factory invalidation
  callbacks in the VL provider.
- [x] **Deferred from the initial outcome:** multiple simultaneous connection
  profiles, a Connection Doctor, deterministic virtual clocks, and exhaustive
  recorded replay tooling.
- [x] **Deferred from the initial outcome:** acknowledged idempotent submission
  until the server's follow-on lifecycle profile is implemented.

The portable manager now owns one immutable profile and matching HTTP/WS
clients. It derives endpoints, refreshes injected tokens, applies bearer auth
to the WebSocket upgrade, and gives side-effect-free HTTP SDK operations
bounded retry/backoff with stable logical request IDs and `Retry-After`
handling. Correlated WebSocket read RPCs now use the same bounded policy for
transient transport failures while retaining one logical `request_id`;
workflow submission and cancellation remain single-attempt. Shared redaction
helpers now cover URIs, headers, known secrets, workflow-input maps, nested
diagnostic values, inline data URIs, portable exception logging, and
host-visible controller errors. The transport seam covers deterministic
connect, token refresh, run, cancel, failure, and active-job reconnect tests
without a live server.
`NodeToolConnectionSession` now owns replaceable manager/profile lifecycle,
status projection, reconnect/reset, and stale-connect rejection for mutable
hosts. VL delegates this neutral lifecycle to the portable session. HTTP
configuration is request-scoped, preserving caller-owned `HttpClient` state
and reverse-proxy deployment subpaths, including storage asset URLs.

### Phase 5A - Portable execution options and timings

- [x] Add AppHost-scoped advanced execution defaults to the VL Connect node
      for persistence, event detail, and asset persistence.
- [x] Negotiate non-default Connect preferences through server capabilities,
      preserve ordinary behavior when unsupported, and allow future
      per-execution overrides without changing server policy.
- [x] Add portable typed execution options for job/session persistence,
      full/output/terminal event detail, and automatic/temporary asset
      persistence.
- [x] Default SDK asset persistence to `Temporary` so generated-asset
      autosave is off; retain explicit `Auto` for durable asset-library
      behavior.
- [x] Negotiate the `temporary_asset_upload` capability and use the additive
      temporary input route for large media without database or thumbnail
      work; retain persistent upload as the safe fallback.
- [x] Negotiate non-default options through capabilities; never assume an
      older/current server supports them.
- [x] Cache the capability document per portable connection generation so
      negotiated low-overhead executions do not add a discovery request per
      run; reconnecting, replacing the profile, or resetting invalidates the
      cache.
- [x] Keep current non-SDK clients unchanged when options are absent; SDK
      clients project their documented temporary-asset default.
- [x] Add safe portable runtime timings for connection, input/media
      preparation, remote execution, and total duration.
- [x] Expose runtime timings through the portable execution result so VL,
      Unity, and console clients share one measurement model.
- [x] **Deferred timing refinement:** add submission acknowledgement and host output-materialization timings
      when those lifecycle boundaries become independently observable.
- [x] **Deferred until benchmarks:** offer named convenience profiles only after server benchmarks establish
      useful combinations; always allow callers to override individual
      options.
- [x] Document that session-only jobs and temporary assets trade durability
      and reconnect/history behavior for lower latency.

### Gate

- [x] Console and VL use the same single-profile connection, capability,
  preflight, retry, and diagnostic services without static-state leakage.

## Phase 6 - Finish the thin VL adapter

- [x] Keep dynamic `IVLNodeDescription` and `IVLNode` factories in the VL
  assembly.
- [x] Keep pin construction and NodeTool-to-VL type mapping in the adapter.
- [x] Map option-constrained workflow and individual-node pins to
  compiler-visible VL dynamic enum types, including defaults, scalar outputs,
  numeric wire literals, and nested spreads.
- [x] Keep generated enum type identities compact enough for useful vvvv
  hover text, and reuse mapped defaults when individual-node instances rebuild
  their minimal pin descriptions.
- [x] Cover primitive, tuple, bytes, file-path, media, structured, enum, and
  spread projections with focused adapter tests.
- [x] Keep rising-edge trigger/cancel handling in the frame `Update()` surface.
- [x] Keep AppHost synchronization-context invalidation and factory refresh.
- [x] Keep reapplication of latched outputs across live recompilation.
- [x] Keep `SKImage` creation/disposal and VL image presentation.
- [x] Move asset URI/media payload parsing, authenticated asset
  materialization, workflow-controller construction, connection-scoped input
  preparation, auto-run scheduling, output-delta tracking, and generic typed
  text/chunk presentation into the portable SDK.
- [x] Reduce `WorkflowNodeBase` to pin creation, frame input collection,
  portable-runtime invocation, host dispatch, snapshot application, and
  VL-specific value/image projection.
- [x] Register connection/catalog services through the AppHost with explicit
  ownership.
- [x] Scope mutable Connect-node endpoint, authentication, execution/media,
  reconnect, discovery, and configuration-error state per AppHost.
- [x] Keep the dynamic-factory assembly free of broad `ImportAsIs` or
  `ImportNamespace` declarations by default.
- [x] Audit vvvv import namespaces/types for accidental helper nodes; the
      assembly registers only its three deliberate dynamic factories.
- [x] **Deferred interactive verification:** verify dynamic node identity and rename/refresh behavior.
- [x] **Deferred interactive verification:** verify patch reload, vvvv restart, server restart, reconnect, and
  disposal.
- [x] Add restrained VL lifecycle logging: emit one concise message when
  connection, discovery, and dynamic factory resolution are ready; otherwise
  emit redacted actionable errors only. Avoid per-frame, per-pin, and routine
  refresh noise.
- [x] **Deferred until fixtures stabilize:** refresh help patches and
      diagnostics nodes.
- [x] **Deferred from the initial outcome:** policies for hypothetical ordinary
  C# nodes; the initial package exposes the existing dynamic factories and
  deliberate helper nodes only.

### Gate

- [x] No protocol, discovery cache, asset URI, or execution state-machine logic
  is implemented only inside `Nodetool.SDK.VL`.
- [x] **Deferred interactive verification:** existing workflow and
      individual-node patches behave correctly after
  reload and live recompilation.

## Phase 7 - Test, package, document, and release VL

- [x] Package the current `VL.Nodetool.vl` document and managed DLLs under the
  `VL.Nodetool` package ID and `lib/net8.0`.
- [x] Provide and pass a current headless VL document compilation test.
- [x] Run portable SDK unit/contract tests and verify the standalone
      `Nodetool.SDK` package does not contain or depend on `Nodetool.Types`.
- [x] Run headless vvvv compilation tests.
- [x] Extract the packed `VL.Nodetool` NuGet into an isolated package
      repository and compile its package-relative VL document without source
      assembly paths.
- [x] **Deferred to the packed-package refinement pass:** run the interactive
  primitives, image, cancellation, lists, audio-file,
  video-file, structured type, and large-reference workflows.
- [x] Verify the deployed package root, `.vl` document, and package identity
  use `VL.Nodetool`.
- [x] Clearly separate `Nodetool.SDK.VL` and `VL.Nodetool`
  package responsibilities.
- [x] Establish one version source for the C# SDK, VL adapter, and VL package
  release set.
- [x] Align the supported `VL.Core`, source document, help patch, and package
  dependency versions.
- [x] Ensure compiled DLLs are packaged under the expected `lib/net8.0`
  location without `.vl` files referencing a `.csproj`.
- [x] **Deferred interactive verification:** verify only intended public types
      appear in the node browser; automated package-surface guards pass.
- [ ] Publish an internal C# SDK prerelease and matching VL package.
- [x] Document supported NodeTool protocol, C# target frameworks, vvvv
  version, installation, diagnostics, and known limitations.
- [x] Record a clean 0.1.6 release test matrix below.
- [x] **Deferred from the initial outcome:** long-running soak automation and
  broad platform/package matrices beyond console, VL, and the initial Unity
  target.

### Exit gate

- [x] Plain C# can discover, preflight, run, observe, cancel, upload, download,
  and inspect results without VL during an active client session.
- [x] VL consumes those same services as a thin adapter.
- [x] Focused server, C#, VL, and headless package suites pass; the broader
      interactive matrix is explicitly deferred.
- [x] The package is the initial portable foundation for the later Unity U0
      compatibility/AOT spike.

## Phase 8 - Optional `VL.Nodetool.HDE` editor companion

This is a small editor convenience layer, not another node or workflow
browser, and is not a gate for the first `VL.Nodetool` release.

- [ ] Package an optional `VL.Nodetool.HDE` extension with one command and a
      dockable status window.
- [ ] Reuse the portable SDK for its connection, capabilities, preflight,
      and errors; keep only HDE window and editor-selection access in the
      extension.
- [ ] Show concise server/SDK readiness and actionable diagnostics.
- [ ] For a selected NodeTool workflow or node, show its resolved contract and
      preflight result.
- [ ] Verify editor-runtime isolation, extension restart, disposal, and that
      exported applications remain independent of the HDE package.

Later possibilities: an isolated execution test surface, writing compatible
test values to selected pins, patch-wide NodeTool validation, and lightweight
asset/output inspection.

## Explicitly deferred

- [x] Unity-specific APIs and objects.
- [x] Runtime-generated arbitrary CLR record assemblies.
- [x] Full offline model runner integration.
- [x] Web/mobile/console transports not required by the initial Unity target.

### Future local fast-media transport

This is an optional same-machine optimization. Remote/cloud execution must
continue to use ordinary encoded media, upload, and referenced-asset paths.

- [ ] Define negotiated media transport modes such as `auto`, `encoded`,
      `shared_memory`, and `shared_texture`, with safe automatic fallback.
- [ ] Start with encoded-byte and existing-file passthrough so hosts can avoid
      re-encoding media they already hold in a portable form.
- [ ] Evaluate a cross-process shared-memory descriptor for raw pixels
      (mapping identity, dimensions, stride, format, generation, ownership,
      and lifetime) as the first broadly reusable local fast path.
- [ ] Treat shared Direct3D textures as a later host/platform adapter:
      exchange an OS-supported shared handle plus synchronization and lifetime
      metadata, never a process-local pointer.
- [ ] Keep transport negotiation and descriptors in the server/C# contract;
      keep vvvv texture creation, graphics-device interop, and disposal in the
      VL adapter.
- [ ] Keep generic workflow `image` inputs mapped to `SKImage` so images
      computed inside vvvv remain first-class; do not change all image inputs
      to file paths.
- [ ] Add an optional server-contract representation hint such as `pixels`,
      `file`, or `asset`; map explicitly file-backed image inputs to VL `Path`.
- [ ] Evaluate a strongly typed `ImageSource` wrapper plus small `Path`,
      `SKImage`, and `Texture` adapter nodes if one universal workflow pin is
      needed. Avoid three competing pins and avoid an untyped `object` pin.
- [ ] Initially adapt `Texture` through explicit readback; allow the same
      adapter to select negotiated shared-texture transport later.
- [ ] Benchmark 2K and 4K passthrough before selecting defaults, including
      encode, transfer, server materialization, download, and host decode time.

### Future asset-output actions

Keep workflow output pins focused on typed results. Saving and persistence are
explicit consumer actions rather than side effects or extra pins repeated on
every generated workflow node.

- [ ] Keep typed `ImageRef`, `AudioRef`, `VideoRef`, and `AssetRef` values as
      the portable asset result and preserve temporary/durable identity,
      content type, metadata, URI, and optional inline data.
- [ ] Retain `Asset As File` for cached local materialization without implying
      user-selected storage or durable server persistence.
- [ ] Add a separate `Save Asset` node with asset, destination `Path`, trigger,
      and overwrite policy inputs plus resulting path, success, and error
      outputs.
- [ ] Keep generic download, atomic copy, extension/MIME handling, overwrite
      policy, and cancellation in the portable C# SDK; keep VL `Path`, trigger,
      and frame projection in the VL adapter.
- [ ] Add `Persist Asset` only after NodeTool exposes an authoritative
      temporary-to-durable promotion operation; return the resulting durable
      asset reference.
- [ ] Add host-native conversion nodes such as `Asset As Image` or
      `Asset As Texture` only where they improve common workflows, using the
      same cached materializer and explicit resource ownership.
- [ ] Do not add `Save To`, overwrite, or persistence pins to every generated
      workflow node, and do not make persistence an accidental connection
      side effect.

## 0.1.6 release snapshot - 2026-07-26

- [x] NodeTool WebSocket SDK preflight/capability tests: 51 passed.
- [x] NodeTool default-on/kill-switch SDK regression suite: 173 passed across
      workflow discovery, capabilities, preflight, lifecycle, HTTP, RPC, and
      tRPC coverage.
- [x] NodeTool WebSocket TypeScript check: passed.
- [x] Live HTTP capabilities, static preflight, and availability preflight:
      passed against the local development server.
- [x] Portable `Nodetool.SDK` tests: 147 passed.
- [x] VL adapter unit tests: 85 passed (media/value policy tests moved to the
      portable suite rather than being dropped).
- [x] Packaged node-surface guards verify exactly the three deliberate
      dynamic factory dependencies and prohibit broad managed-type import
      attributes; visual node-browser inspection remains interactive.
- [x] Headless primary/help/isolated-package VL document tests: 3 passed.
- [x] Live WebSocket discovery verified against the development server:
      2,527 node types, 76 valid workflow descriptors, and asset list/get.
- [x] Portable connection/session ownership, endpoint derivation, token
      providers, diagnostic redaction, bounded read-only retries, stable read
      request identity, and shared runtime timing snapshots are implemented
      and covered by focused tests.
- [x] Portable `Nodetool.SDK.0.1.6.nupkg`: built and dependency boundary
      verified. SHA-256:
      `E9D84271ED5EAD1B05B6E88D8B6AF14231BFBE1515123F530F76B0FC1D14B5B1`.
- [x] `VL.Nodetool.0.1.6.nupkg`: built; required entries, non-empty
      assemblies, and package-relative VL assembly references verified.
      SHA-256:
      `8EE8BCC1558D7031388F2D648BEAA8F32FD615A92069A144E716001D285C4723`.
- [x] **Deferred:** interactive full media/reload/reconnect matrix moves to refinement
      testing after this first release candidate.
- [ ] Remote NuGet publication: not performed; requires release credentials
      and an explicit publish decision.

## Recommended follow-ups, in priority order

1. Run the interactive primitives, image, lists/enums, audio path, video path,
   cancellation, timeout, reload, server-restart, and reconnect matrix against
   the packed `VL.Nodetool.0.1.6` package rather than source assemblies.
2. Inspect the visible node-browser surface from the isolated packed-package
   consumer and confirm only the deliberate dynamic factories/helpers appear.
3. Refresh the help patch after the interactive fixture set stabilizes.
4. Publish matching `Nodetool.SDK` and `VL.Nodetool` prereleases, then add CI
   packaging/publish jobs and release notes.
5. Revisit AOT/source-generated JSON and removal of reflection-heavy generated
   type activation before beginning Unity/IL2CPP work.
6. Begin the Unity plan only in a separate future work session.
