# vvvv SDK Current API and Improvement Plan

## Goal

Restore reliable NodeTool workflow discovery, VL node creation, and workflow execution in vvvv against the current NodeTool backend. The backend work must be additive and isolated so it does not alter the behavior of current REST, tRPC, WebSocket, web, Electron, or other clients.

The preferred end state is:

1. The backend provides a versioned, authoritative workflow interface for SDK clients, initially protected by a rollout flag.
2. The C# SDK consumes that interface as a required, versioned contract and does not independently infer workflow I/O from graphs.
3. Existing workflow response shapes and execution commands continue to work unchanged.
4. Contract drift is detected by automated tests before publishing a NodeTool or SDK release.

## Verified baseline

- [x] Confirm that the current TypeScript workflow response explicitly returns `input_schema: null` and `output_schema: null`.
- [x] Confirm that the REST workflow routes are compatibility routes over the current TypeScript workflow implementation, rather than an independent source of richer schemas.
- [x] Confirm that WebSocket `list_workflows` and `get_workflow` delegate to the same tRPC workflow procedures and therefore return the same null schemas.
- [x] Confirm that no current `workflow_interface` or equivalent full workflow-I/O endpoint exists.
- [x] Confirm that the current C# SDK graph inference is partial and should be replaced by the authoritative backend workflow-interface contract.
- [x] Confirm that current workflow graphs can contain node values under `data` and/or `properties`, while the SDK workflow DTO only models `data`.
- [x] Confirm that current REST node metadata uses a bare array, while correlated WebSocket RPC wraps the list under `result.nodes`; keep these transport DTOs distinct.
- [x] Confirm that terminal WebSocket job results currently use a nested `result.outputs` shape that the SDK does not reconcile correctly.
- [x] Capture live REST and WebSocket payloads from the available Electron backend. The running app is a stale May bundle because its July nightly Python-package synchronization failed; its differences from current source are recorded below and are not treated as compatibility requirements.

### Live validation notes — 2026-07-23

- The Electron backend at `127.0.0.1:7777` reports `/health` successfully but `/api/health` returns `version: "unknown"`.
- Both the bundled and current development REST node-metadata routes return a bare array. The `{ nodes: [...] }` shape belongs to the correlated WebSocket RPC result.
- `GET /api/workflows` returned about 37.5 MB because list responses include workflow graphs. Explicit `limit=1` reduced the response to 600 bytes.
- REST workflow pagination returned `next`, but supplying it as `cursor` repeated page one. Current REST and tRPC source accepted the cursor field without forwarding it to `Workflow.paginate.startKey`; this is fixed in the implementation increment.
- Correlated WebSocket RPC requires `request_id` at the command-envelope top level. The SDK incorrectly nested it inside `data`; this is fixed and contract-tested.
- The full `list_nodes` WebSocket response arrived in about 0.8 seconds and was about 6.36 MB, but used msgpackr extension type `0x00` for JavaScript `undefined`. MessagePack-CSharp rejects that private extension. The backend binary send path now emits standard MessagePack maps and encodes `undefined` as `nil`.
- The Electron nightly package updater attempted the invalid PEP 440 pin `nodetool-core==0.7.1-nightly.20260714.696`, so the running bundle is useful as a drift fixture but not as the target current server.

## Current-client safety rules

- [x] Do not change the existing default shape of `GET /api/workflows`, `GET /api/workflows/:id`, tRPC `workflows.list/get`, or WebSocket `list_workflows/get_workflow` during this work.
- [x] Do not repurpose the existing `input_schema` and `output_schema` fields in the first rollout; clients may have encoded assumptions about their current null values.
- [ ] Add new behavior through a versioned SDK-specific capability, endpoint, and WebSocket command.
- [x] Keep the new backend capability disabled by a server feature flag during the first rollout.
- [x] Require workflow-interface v1 in the new C# SDK and report an explicit incompatible-server/feature-disabled error when it is unavailable.
- [x] Keep the backend workflow-interface derivation pure and independently testable.
- [ ] Do not make backend availability a prerequisite for loading the vvvv package or opening an existing patch.
- [ ] Do not add compatibility code for past NodeTool server or SDK contracts.

## Proposed versioned workflow-interface contract

Add a new contract instead of changing existing workflow responses.

### Backend surface

- REST: `GET /api/workflows/:id/interface?version=1`
- REST bulk: `POST /api/sdk/v1/workflow-interfaces` with `{ version: 1, ids: [...] }`
- tRPC: `workflows.interface({ id, version: 1 })`
- tRPC bulk: `workflows.interfaces({ ids, version: 1 })`
- WebSocket RPC: `get_workflow_interface` with `{ id, version: 1, request_id }`
- WebSocket bulk RPC: `get_workflow_interfaces` with `{ ids, version: 1, request_id }`
- Server feature flag: `NODETOOL_ENABLE_SDK_WORKFLOW_INTERFACE_V1`

The REST and WebSocket variants must call the same service function and return the same logical payload. Bulk requests are bounded to a documented maximum (initially 100 workflow IDs) and return per-workflow results/diagnostics so one invalid workflow does not discard the full page.

Workflow discovery must never require downloading workflow graphs. Discovery pages contain only slim workflow summaries (`id`, display name, `etag`, and other small identity metadata), followed by bounded interface batches. This is a payload-shape requirement, not merely a pagination optimization: graphs can be very large and may contain inline image or other media data.

### SDK requirement

The new SDK requires workflow-interface v1. It may discover the interface over REST or the correlated WebSocket RPC, but both transports must expose the same versioned payload. A missing endpoint, unknown command, disabled server feature, or unsupported version is a clear compatibility error; the SDK does not infer the interface locally.

### Response shape

Use NodeTool `TypeMetadata`-style data instead of lossy JSON Schema wherever possible:

```json
{
  "version": 1,
  "workflow_id": "...",
  "etag": "...",
  "source": "server",
  "inputs": [
    {
      "node_id": "...",
      "name": "prompt",
      "description": "...",
      "required": true,
      "default": "",
      "type": {
        "type": "str",
        "optional": false,
        "type_args": [],
        "type_name": null,
        "values": null
      }
    }
  ],
  "outputs": [
    {
      "node_id": "...",
      "name": "image",
      "description": "...",
      "stream": false,
      "type": {
        "type": "image",
        "optional": false,
        "type_args": [],
        "type_name": "ImageRef",
        "values": null
      }
    }
  ],
  "diagnostics": []
}
```

- [x] Finalize the v1 schema in `@nodetool-ai/protocol` before implementing either client.
- [x] Finalize a bounded bulk response envelope containing `interfaces` and per-workflow `errors`.
- [x] Include stable workflow and node IDs so output routing never depends only on display names.
- [x] Include the workflow `etag` so SDK discovery can avoid rebuilding unchanged VL descriptions.
- [x] Preserve enum `values`, enum/type identity, nested `type_args`, optionality, stream information, defaults, descriptions, and structured NodeTool type names.
- [x] Bound serialized defaults in discovery interfaces (initially 16 KiB per default); omit oversized inline image/media values and return a diagnostic.
- [ ] Define and test maximum single-interface, bulk-response, and total discovery-page byte sizes.
- [x] Return diagnostics for unresolved or ambiguous types rather than silently converting them to strings.
- [x] Define deterministic behavior for duplicate input/output names and reject invalid interfaces from VL node creation with actionable diagnostics.

## Phase 0 — Current contract fixtures and safety net

**Outcome:** We can reproduce the current supported behavior without launching vvvv.

- [x] Add `csharp/Nodetool.SDK.Tests/Nodetool.SDK.Tests.csproj`.
- [x] Add the test project to the normal SDK verification script.
- [ ] Capture current REST fixtures for node metadata, workflow list, workflow detail, workflow pagination, and errors.
- [ ] Capture current MessagePack fixtures for RPC responses, `job_update`, `node_update`, `output_update`, cancellation, and server errors.
- [ ] Add current fixtures for `{ nodes: [...] }`, null workflow schemas, and graph nodes using `properties`.
- [ ] Add fixtures covering both `data` and `properties` on the same node and define precedence explicitly.
- [ ] Add representative workflows for string, integer, float, boolean, enum, list, image, audio, video, document, structured object, dynamic output, and streamed text.
- [ ] Add a test proving that existing REST workflow responses remain byte-shape compatible when the backend feature flag is off.
- [ ] Document the single supported NodeTool server and SDK protocol version pair.

### Phase 0 acceptance gate

- [ ] All captured fixtures deserialize deterministically.
- [ ] Tests demonstrate the known current failures before their fixes are applied.
- [ ] No existing NodeTool routes or response schemas have changed.

## Phase 1 — C# current wire contract and normalized domain models

**Outcome:** Transport changes no longer leak into discovery, node creation, or execution.

- [ ] Separate wire DTOs from stable SDK domain models.
- [x] Preserve the current bare-array REST metadata contract and normalize the distinct WebSocket `result.nodes` envelope.
- [ ] Normalize graph node properties from `properties`, falling back to `data`; preserve both raw forms for diagnostics.
- [ ] Normalize edge handles, dynamic properties, dynamic outputs, UI properties, and optional IDs.
- [ ] Add tolerant readers for unknown fields and strict validation for required identity fields.
- [x] Normalize workflow list pagination in the HTTP SDK, follow `next`, and reject repeated cursors instead of looping indefinitely.
- [x] Normalize terminal job results from the current `result.outputs` shape.
- [x] Add current streaming fields such as `disposition` and `done` to the C# protocol models.
- [ ] Replace silent empty-list fallbacks on malformed contracts with typed SDK exceptions carrying endpoint, command, and payload-shape context.
- [x] Remove active usage of the unimplemented HTTP node/workflow-execution endpoints; VL execution uses the current WebSocket worker protocol exclusively.
- [ ] Remove obsolete DTOs and execution helpers that implement unsupported past contracts.

### Phase 1 acceptance gate

- [ ] The SDK reads all Phase 0 current fixtures.
- [x] REST node discovery succeeds for the current bare-array payload, and WebSocket discovery reads `result.nodes` from general MessagePack map types.
- [ ] All graph shapes permitted by the current protocol produce the expected normalized graph.
- [ ] No VL-specific assembly is required by these tests.

## Phase 2 — Authoritative backend workflow-interface derivation

**Outcome:** The backend is the single source of truth for the workflow interface.

Implement the algorithm as a clearly specified pure TypeScript operation. The C# SDK consumes its result and does not duplicate these graph/registry rules.

### Input derivation

- [x] Identify the currently supported `nodetool.input.*` nodes by namespace.
- [x] Resolve `name`, description/label, bounded default, min/max, required state, and input node ID from normalized properties.
- [x] Resolve input types from node metadata rather than from a hardcoded node-name switch whenever metadata is available.
- [ ] Support dynamic property metadata and dynamic input nodes.
- [x] Fail with a diagnostic when required node metadata is unavailable; do not guess from an unsupported historical contract.

### Output derivation

- [ ] Identify generic `nodetool.output.Output` and dedicated output nodes such as image, audio, and video outputs.
- [x] Resolve the public output name and output node ID from normalized properties.
- [x] Follow incoming data edges to the source node and source handle.
- [x] Resolve the source handle through static or dynamic node output metadata.
- [x] Preserve enum identity, list element types, structured NodeTool types, optionality, and streaming flags when present in metadata.
- [x] Do not use edge UI class names as authoritative type metadata.
- [x] Report missing source nodes, missing handles, multiple incoming value edges, duplicate public names, and unknown node types as diagnostics.

### Contract fixtures

- [x] Add focused TypeScript input/expected-output tests for the derivation algorithm, including `properties` precedence, enum metadata, duplicates, unresolved types, and an oversized inline image default.
- [x] Verify that the C# workflow-interface DTOs deserialize representative typed server results without applying graph inference.
- [x] Require the SDK to preserve the server's public names, IDs, types, defaults, and diagnostics.

### Phase 2 acceptance gate

- [ ] The current example workflows produce useful interfaces without stored schemas.
- [ ] Stored schemas do not override the authoritative v1 interface unless the v1 contract explicitly defines such a source.
- [ ] Type disagreements are visible in diagnostics and never silently downgraded without explanation.

## Phase 3 — Additive backend capability

**Outcome:** New SDK clients can request an authoritative interface without changing existing APIs.

- [x] Add the v1 workflow-interface schemas and types to `@nodetool-ai/protocol`.
- [x] Implement a registry-aware workflow-interface service outside the HTTP/tRPC router layer.
- [x] Add protected tRPC `workflows.interface` using the existing workflow viewer authorization rules.
- [x] Add protected, bounded tRPC `workflows.interfaces` for one discovery page.
- [x] Add a slim, cursor-paginated SDK workflow-summary query that selects only identity/revision columns and never serializes graph nodes, edges, or inline media.
- [x] Add `GET /api/workflows/:id/interface?version=1` as a thin bridge to the same service.
- [x] Add `POST /api/sdk/v1/workflow-interfaces` as the bounded REST bulk bridge.
- [ ] Add WebSocket `get_workflow_interface` as a thin RPC bridge to the same service.
- [ ] Add WebSocket `get_workflow_interfaces` as the bounded bulk RPC bridge.
- [ ] Guard all three entry points with `NODETOOL_ENABLE_SDK_WORKFLOW_INTERFACE_V1` for the initial rollout. (tRPC and REST are guarded; WebSocket remains.)
- [x] Return a stable feature-disabled/not-supported API error that the SDK can recognize.
- [x] Do not populate or alter existing workflow `input_schema`/`output_schema` fields in this phase.
- [ ] Add authorization tests for owner, collaborator viewer, public workflow, unauthorized user, and missing workflow.
- [ ] Add parity tests proving REST, tRPC, and WebSocket return equivalent v1 payloads.
- [ ] Add tests for TypeScript-native nodes, Python-bridge nodes, dynamic nodes, and unavailable node packs.
- [x] Bound bulk computation to 100 workflows and cache derived interfaces by workflow `etag` plus node-registry revision, with a 512-entry per-registry cap.

### Phase 3 acceptance gate

- [ ] Feature flag off: all existing tests pass and the new surface is unavailable in the documented way.
- [ ] Feature flag on: existing workflow responses are unchanged and v1 interface tests pass.
- [ ] No existing web or Electron call site needs modification to consume the new feature.

## Phase 4 — Discovery and VL factory lifecycle

**Outcome:** vvvv loads quickly, refreshes safely, and retains usable nodes through transient failures.

- [x] Add typed C# REST DTOs and client methods for compact paginated workflow summaries and authoritative workflow-interface v1 responses.
- [x] Convert missing, disabled, and unsupported workflow-interface REST responses into an explicit SDK compatibility exception.
- [x] Make HTTP discovery the default bootstrap transport; do not require an open execution socket to discover nodes and workflows.
- [x] Request workflow-interface v1 in bounded batches of at most 100 workflows.
- [x] Never call the graph-bearing workflow list/detail routes during routine SDK discovery.
- [x] Surface one clear incompatible-server/feature-disabled status on REST 404, feature-disabled, or unsupported version; do not create partially inferred workflow nodes.
- [x] Follow workflow pagination until completion while rejecting repeated cursors.
- [x] Remove the graph-bearing list-then-sequential-detail discovery path entirely; pagination alone is insufficient for image-heavy graphs.
- [x] Use the single-workflow interface request only for diagnostics, targeted refresh, or a changed workflow.
- [ ] Cache normalized metadata and workflow interfaces by ID and `etag`.
- [ ] Replace synchronous `Task.Run(...).Wait(...)` factory initialization with asynchronous stale-while-revalidate loading.
- [ ] Keep the last successful factory contents when refresh fails.
- [x] Do not permanently cache an empty factory after a transient startup failure.
- [ ] Add explicit `Refresh`, `Last Refresh`, `Server Version`, `Interface Source`, and `Last Error` diagnostics for vvvv.
- [ ] Invalidate only changed workflow node descriptions.
- [ ] Debounce rapid server/workflow changes so vvvv is not repeatedly rebuilding the factory.
- [ ] Ensure changing the Connect node endpoint/auth resets both discovery and execution state exactly once.

### Phase 4 acceptance gate

- [x] Opening vvvv with NodeTool offline does not block for 30 seconds (discovery is cancelled after five seconds and retried later).
- [ ] Starting NodeTool after vvvv makes workflows discoverable without restarting vvvv.
- [ ] Stopping NodeTool leaves the last successful workflow nodes available with a stale/error indicator.
- [ ] Renaming or editing one workflow refreshes only the affected description.
- [ ] Discovery performance is measured with 10, 100, and 1,000 workflows.
- [ ] Discovery memory and payload sizes are measured with large graphs containing inline image data, proving graph size does not affect discovery response size.

## Phase 5 — Reliable VL node creation and type binding

**Outcome:** Generated workflow nodes have stable identities and accurate, useful pins.

- [ ] Use workflow ID as the stable internal identity; treat workflow name as display metadata.
- [x] Define deterministic duplicate-name handling using a short workflow-ID suffix.
- [x] Generate input and output pins exclusively from the normalized workflow interface.
- [ ] Preserve defaults, min/max ranges, descriptions, required/optional state, enums, and list element types.
- [x] Map primitive NodeTool types to native VL types.
- [x] Map lists to appropriate spreads/arrays rather than always using `string[]`.
- [ ] Bind structured NodeTool types through the C# type registry where a generated type exists.
- [ ] Map image inputs/outputs to the selected VL image type and document ownership/disposal rules.
- [ ] Define corresponding audio, video, document, and generic asset-reference mappings.
- [x] Use an explicit JSON/object fallback pin for unsupported types instead of silently pretending they are strings.
- [x] Surface per-workflow diagnostics when a pin uses a fallback type.
- [ ] Keep standard execution pins (`Trigger`, `Cancel`, `AutoRun`, status, error) stable for existing patches.
- [ ] Add tests that load representative v1 `.vl` patches and verify node/pin resolution.
- [ ] Align `VL.Core` versions between the C# project and nuspec before publishing.

### Phase 5 acceptance gate

- [ ] All Phase 0 workflow fixtures produce the expected VL pin names and CLR types.
- [ ] Existing workflow nodes remain resolvable by identity after a workflow display-name change.
- [ ] Image, audio, video, enum, structured type, and list workflows no longer degrade silently to strings.

## Phase 6 — Workflow execution and output reconciliation

**Outcome:** Fast, streamed, media-heavy, cancelled, and reconnecting workflows complete predictably.

- [x] Create/bind the execution session and event buffer before sending `run_job` so fast events cannot be lost before subscribers attach.
- [x] Replace `async void` workflow execution with a tracked task and explicit exception propagation.
- [x] Route bound execution updates by server job ID; accept unscoped updates only when exactly one bound session exists.
- [x] Maintain a per-run output routing table keyed by output node ID, public output name, `node_name`, and `output_name`.
- [x] Apply `output_update.disposition` (`append` versus `replace`) and `done` semantics.
- [x] Treat live output updates as progressive state and terminal `result.outputs` as authoritative reconciliation.
- [x] Correctly unwrap the current nested terminal result.
- [x] Preserve the latest valid media value if a terminal result contains only a URI/reference and no inline bytes.
- [x] Use the workflow interface type when encoding every input, including graph-derived image/audio/video/document inputs.
- [x] Prefer asset/reference transport for large media; set and test explicit inline payload limits.
- [x] Make execution timeout configurable globally and per node.
- [x] Support cancellation while server-queued; client-generated job IDs allow immediate, exactly-once cancellation of queued and running jobs.
- [ ] Add reconnect/replay behavior using `reconnect_job` for interrupted sockets.
- [x] Never invoke user/VL completion callbacks while holding an execution-session lock.
- [ ] Marshal pin/state changes through the correct VL update context.
- [ ] Separate recoverable transport errors from workflow/node failures in diagnostics.

### Phase 6 acceptance gate

- [x] A workflow completing immediately after `run_job` still populates all outputs.
- [ ] Append and replace streams produce the expected final VL value.
- [x] Terminal reconciliation fills outputs when one or more live updates were missed.
- [ ] Concurrent runs do not cross-route updates.
- [ ] Cancellation and reconnect tests leave no permanently running VL state.
- [ ] Large image/media workflows stay below the documented memory and MessagePack payload bounds.

## Phase 7 — End-to-end validation and rollout

**Outcome:** The change is deployable incrementally and reversible without breaking other NodeTool clients.

- [ ] Upgrade the SDK and generated-types projects from the vulnerable `MessagePack` 3.0.300 dependency to a patched release, guarded by binary protocol round-trip fixtures.

- [ ] Add a backend integration harness that boots NodeTool with the feature flag both off and on.
- [ ] Test the required workflow-interface v1 contract over REST and correlated WebSocket RPC in CI.
- [ ] Add vvvv smoke patches for discovery, primitive workflow execution, streaming text, image input/output, cancellation, and refresh.
- [ ] Run NodeTool protocol, websocket, workflow, web, and Electron test suites affected by the additive backend route/command.
- [ ] Run SDK unit tests, C# builds, VL builds, package creation, and package-content verification.
- [ ] Verify existing `.vl` test patches against the newly built DLLs/NuGet package.
- [ ] Publish an internal prerelease with the server feature flag disabled by default.
- [ ] Enable the backend flag in development/nightly builds and collect interface diagnostics.
- [ ] Validate server-derived interfaces against the Phase 2 expected-output fixtures in development/nightly builds.
- [ ] Resolve all interface diagnostics and fixture differences before enabling the SDK backend feature by default.
- [ ] Enable the backend feature for the SDK release after shadow validation passes.
- [ ] Document rollback: disable the backend flag to remove the new SDK surface; existing current NodeTool clients remain unaffected, while the v1 SDK reports that its required backend feature is unavailable.
- [ ] Update the SDK, C# SDK, VL, vvvv, and troubleshooting READMEs.

### Final acceptance gate

- [ ] Existing NodeTool clients behave identically with the backend flag off and on.
- [ ] The declared current NodeTool server and SDK versions work together through workflow-interface v1.
- [ ] Workflow discovery, node creation, refresh, execution, streaming, cancellation, and media conversion have automated coverage.
- [ ] No required work depends on stored `input_schema` or `output_schema` being populated.
- [ ] The server-derived interface is the sole authoritative workflow I/O contract used by the SDK.

## Likely implementation locations

### NodeTool backend

- `packages/protocol/src/api-schemas/workflows.ts`
- `packages/protocol/src/messages.ts`
- a new shared workflow-interface derivation module in an appropriate protocol/core package
- `packages/websocket/src/trpc/routers/workflows.ts`
- `packages/websocket/src/routes/workflows.ts`
- `packages/websocket/src/unified-websocket-runner.ts`
- corresponding protocol, tRPC, REST, WebSocket, authorization, and parity tests

### C# SDK

- `csharp/Nodetool.SDK/Api/Models/`
- `csharp/Nodetool.SDK/Api/NodetoolClient.cs`
- `csharp/Nodetool.SDK/Execution/NodeToolExecutionClient.cs`
- `csharp/Nodetool.SDK/Execution/ExecutionSession.cs`
- `csharp/Nodetool.SDK/Types/WebSocketMessages.cs`
- a new normalized workflow-interface/domain service
- a new C# contract/unit test project

### VL/vvvv integration

- `csharp/Nodetool.SDK.VL/Services/WorkflowMetadataService.cs`
- `csharp/Nodetool.SDK.VL/Factories/WorkflowNodeFactory.cs`
- `csharp/Nodetool.SDK.VL/Nodes/WorkflowNodeDescription.cs`
- `csharp/Nodetool.SDK.VL/Nodes/WorkflowNodeBase.cs`
- `csharp/Nodetool.SDK.VL/Utilities/VlTypeMapping.cs`
- `csharp/Nodetool.SDK.VL/Utilities/VlValueConversion.cs`
- `vvvv-tests/`

## Explicit non-goals for the first release

- [ ] Do not migrate or rewrite stored workflow graphs.
- [ ] Do not require existing workflows to save generated schemas.
- [ ] Do not make the new endpoint mandatory for non-SDK clients running workflows.
- [ ] Do not redesign the NodeTool execution kernel as part of the SDK repair.
- [ ] Do not generate arbitrary CLR record/enum assemblies at runtime until the stable type-binding layer and package policy are settled.
