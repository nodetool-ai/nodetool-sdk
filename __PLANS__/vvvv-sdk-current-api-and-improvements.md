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

### Implementation audit — 2026-07-23

- 135 of 183 checkable items are complete after reviewing the implementation, commits, and targeted test results in both repositories.
- The focused C# suite passes 39 tests. The gamma 7.1 headless VL suite compiles both shipped VL documents with the live NodeTool metadata service; offline compilation of documents that reference dynamic individual nodes remains a separate cache/packaging decision. The NodeTool workflow-interface, WebSocket/interface, and protocol suites pass their targeted tests.
- Full NodeTool package verification remains blocked by pre-existing Sharp TypeScript import errors in the runtime and WebSocket packages. The unrelated package-manifest and lockfile changes in the working tree are not part of this plan.
- The open Phase 1 graph-normalization items describe a legacy client-side inference path that authoritative workflow-interface v1 no longer uses. Before implementing them, decide whether to delete the remaining public legacy graph DTOs or retain them as a separate, non-workflow API.
- The cross-transport flag-on/flag-off integration harness is complete. The highest-value remaining proof is the representative headless and interactive vvvv smoke suite.
- The transport integration harness found and fixed an absent-field drift where MessagePack encoded an explicit JavaScript `undefined` as `nil` while REST omitted the field.
- The C# VL project, package manifest, and main VL document now share the first release target, vvvv gamma 7.1 / `VL.Core 2025.7.1`; the complete supported version pair and manual test preparation are documented in `vvvv-tests/README.md`.
- All seven historical `vvvv-tests/*.vl` files were audited under gamma 7.1. They depend on retired factory names or local workflow nodes and old pin contracts, so they remain migration evidence rather than release smoke tests.
- The live hybrid registry currently exposes 2,527 node definitions and 100 distinct recursive type tokens. It combines TypeScript registrations, Python package JSON metadata, and bridge-only Python metadata. The bridge merge is now centralized, preserves existing TypeScript/package metadata, maps current dynamic/streaming flags, and advances the shared registry revision when bridge-only nodes appear.
- The current node metadata contract names structured types but does not provide a global structured-type schema catalog (`type_name` was absent from the live registry response). A future catalog can enumerate availability and recursive type usage, but generated CLR shapes still require an authoritative schema source rather than inference from names.
- The current VL factory API supports an invalidation observable. Workflow discovery now publishes immutable snapshots asynchronously, marshals invalidation through the AppHost synchronization context, coalesces rapid refresh requests, retries startup discovery, and retains the last successful snapshot after later failures.
- A flag-gated SDK node/type inventory now reports registry revision, Python-bridge readiness, TypeScript/Python metadata provenance, recursive pin-type usage, and unavailable packs. REST and correlated WebSocket pages are capped at 100 types, with bounded enum values and examples; this remains a usage catalog rather than an inferred structured-type schema catalog.
- Live validation of the type inventory reports 2,527 TypeScript nodes, 1,051 distinct structural type signatures, registry revision 2,528, and explicit `python_bridge_ready: false`. This proves the inventory does not silently imply Python coverage when the bridge is unavailable; bridge-ready validation remains open.
- The current `IVLNodeDescription` API has no identity field separate from `Name`, `Category`, and pins, and `NodeDescriptionComparer` uses those values. Workflow-ID-stable rename behavior therefore requires an explicit stable-name or alias/migration policy rather than a hidden factory key.
- Individual-node discovery now performs background stale-while-revalidate refresh, retains the last successful snapshot, retries transient startup failures, and has a five-second maximum initial grace period so existing `.vl` documents can resolve nodes from a fast local server without the previous 30-second offline stall.
- The node-sdk mutation command is currently blocked before mutation by eight pre-existing Windows-only dry-run failures in metadata caching and pack path assertions. Focused registry/inventory tests pass; this blocker is separate from the existing Sharp typecheck failure.

## Current-client safety rules

- [x] Do not change the existing default shape of `GET /api/workflows`, `GET /api/workflows/:id`, tRPC `workflows.list/get`, or WebSocket `list_workflows/get_workflow` during this work.
- [x] Do not repurpose the existing `input_schema` and `output_schema` fields in the first rollout; clients may have encoded assumptions about their current null values.
- [x] Add new behavior through a versioned SDK-specific capability, endpoint, and WebSocket command.
- [x] Keep the new backend capability disabled by a server feature flag during the first rollout.
- [x] Require workflow-interface v1 in the new C# SDK and report an explicit incompatible-server/feature-disabled error when it is unavailable.
- [x] Keep the backend workflow-interface derivation pure and independently testable.
- [x] Do not make backend availability a prerequisite for loading the vvvv package or opening an existing patch.
- [x] Do not add compatibility code for past NodeTool server or SDK contracts.

## Proposed versioned workflow-interface contract

Add a new contract instead of changing existing workflow responses.

### Backend surface

- REST: `GET /api/workflows/:id/interface?version=1`
- REST bulk: `POST /api/sdk/v1/workflow-interfaces` with `{ version: 1, ids: [...] }`
- tRPC: `workflows.interface({ id, version: 1 })`
- tRPC bulk: `workflows.interfaces({ ids, version: 1 })`
- WebSocket RPC: `get_workflow_interface` with `data: { id, version: 1 }` and a top-level `request_id`
- REST: `GET /api/sdk/v1/node-types?cursor=0&limit=100`
- WebSocket RPC: `get_node_type_inventory` with bounded `cursor` and `limit`
- WebSocket bulk RPC: `get_workflow_interfaces` with `data: { ids, version: 1 }` and a top-level `request_id`
- WebSocket summaries RPC: `list_workflow_summaries` with `data: { limit, cursor }` and a top-level `request_id`
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
- [x] Add a test proving that existing REST workflow responses remain byte-shape compatible when the backend feature flag is off.
- [x] Document the single supported NodeTool server and SDK protocol version pair.

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
- [x] Support dynamic property metadata and dynamic input nodes.
- [x] Fail with a diagnostic when required node metadata is unavailable; do not guess from an unsupported historical contract.

### Output derivation

- [x] Identify generic `nodetool.output.Output` and dedicated image, audio, and video output nodes.
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
- [x] Stored schemas do not override the authoritative v1 interface unless the v1 contract explicitly defines such a source.
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
- [x] Add WebSocket `get_workflow_interface` as a thin RPC bridge to the same service.
- [x] Add WebSocket `get_workflow_interfaces` as the bounded bulk RPC bridge.
- [x] Add WebSocket `list_workflow_summaries` without graph or inline-media payloads.
- [x] Guard every SDK workflow-interface entry point with `NODETOOL_ENABLE_SDK_WORKFLOW_INTERFACE_V1` for the initial rollout.
- [x] Return a stable feature-disabled/not-supported API error that the SDK can recognize.
- [x] Do not populate or alter existing workflow `input_schema`/`output_schema` fields in this phase.
- [x] Add authorization tests for owner, collaborator viewer, public workflow, unauthorized user, and missing workflow.
- [x] Add parity tests proving REST, tRPC, and WebSocket return equivalent v1 payloads.
- [x] Add tests for TypeScript-native nodes, Python-bridge nodes, dynamic nodes, and unavailable node packs.
- [x] Centralize the live Python-bridge metadata merge into the shared registry and normalize dynamic-input plus streaming-input/output flags without overwriting TypeScript/package metadata.
- [x] Add a flag-gated, bounded SDK node/type inventory that reports registry revision, readiness/provenance, recursive type usage, and unavailable-pack diagnostics without returning one unbounded multi-megabyte response.
- [x] Bound bulk computation to 100 workflows and cache derived interfaces by workflow `etag` plus node-registry revision, with a 512-entry per-registry cap.

### Phase 3 acceptance gate

- [ ] Feature flag off: all existing tests pass and the new surface is unavailable in the documented way.
- [ ] Feature flag on: existing workflow responses are unchanged and v1 interface tests pass.
- [x] No existing web or Electron call site needs modification to consume the new feature.

## Phase 4 — Discovery and VL factory lifecycle

**Outcome:** vvvv loads quickly, refreshes safely, and retains usable nodes through transient failures.

- [x] Add typed C# REST DTOs and client methods for compact paginated workflow summaries and authoritative workflow-interface v1 responses.
- [x] Convert missing, disabled, and unsupported workflow-interface REST responses into an explicit SDK compatibility exception.
- [x] Make HTTP discovery the default bootstrap transport; do not require an open execution socket to discover nodes and workflows.
- [x] Add an opt-in Connect-node flag that switches workflow discovery to compact correlated WebSocket RPC after the shared socket connects.
- [x] Request workflow-interface v1 in bounded batches of at most 100 workflows.
- [x] Never call the graph-bearing workflow list/detail routes during routine SDK discovery.
- [x] Surface one clear incompatible-server/feature-disabled status on REST 404, feature-disabled, or unsupported version; do not create partially inferred workflow nodes.
- [x] Follow workflow pagination until completion while rejecting repeated cursors.
- [x] Remove the graph-bearing list-then-sequential-detail discovery path entirely; pagination alone is insufficient for image-heavy graphs.
- [x] Use the single-workflow interface request only for diagnostics, targeted refresh, or a changed workflow.
- [x] Cache normalized metadata and workflow interfaces by workflow ID, workflow revision, node-registry revision, and authoritative interface `etag`.
- [x] Replace synchronous `GetAwaiter().GetResult()` factory initialization with asynchronous stale-while-revalidate loading.
- [x] Replace the individual-node factory's unbounded 30-second startup wait with background stale-while-revalidate loading and a five-second maximum first-snapshot grace period.
- [x] Give the workflow factory the same bounded five-second first-snapshot grace period so existing patches can resolve against a fast local backend.
- [x] Keep the last successful factory contents when refresh fails.
- [x] Do not permanently cache an empty factory after a transient startup failure.
- [x] Retain the last workflow snapshot across connection resets and require two consecutive empty discovery responses before publishing an empty factory.
- [x] Add explicit `Refresh`, `Last Refresh`, `Server Version`, `Interface Source`, and `Last Error` diagnostics for vvvv.
- [x] Reuse unchanged workflow node descriptions and replace only descriptions whose workflow revision, registry revision, interface etag, or generated name changed.
- [x] Debounce rapid server/workflow changes so vvvv is not repeatedly rebuilding the factory.
- [x] Ensure changing the Connect node endpoint/auth resets both discovery and execution state exactly once.

### Phase 4 acceptance gate

- [x] Opening vvvv with NodeTool offline does not block for 30 seconds (discovery is cancelled after five seconds and retried later).
- [ ] Starting NodeTool after vvvv makes workflows discoverable without restarting vvvv.
- [ ] Stopping NodeTool leaves the last successful workflow nodes available with a stale/error indicator.
- [ ] Renaming or editing one workflow refreshes only the affected description.
- [ ] Discovery performance is measured with 10, 100, and 1,000 workflows.
- [ ] Discovery memory and payload sizes are measured with large graphs containing inline image data, proving graph size does not affect discovery response size.

## Phase 5 — Reliable VL node creation and type binding

**Outcome:** Generated workflow nodes have stable identities and accurate, useful pins.

- [x] Verify current VL identity semantics: node descriptions expose no identity separate from name/category/pins, so workflow ID cannot be attached as a transparent internal key.
- [ ] Choose and implement an explicit workflow rename strategy (stable ID-derived node name, retained alias, or patch migration) before claiming rename-safe identity.
- [x] Define deterministic duplicate-name handling using a short workflow-ID suffix.
- [x] Generate input and output pins exclusively from the normalized workflow interface.
- [x] Preserve defaults, min/max ranges, descriptions, required/optional state, enum values, and list element types in normalized metadata and VL pin diagnostics.
- [x] Map primitive NodeTool types to native VL types.
- [x] Map list pins to immutable VL-native `Spread<T>` values for both workflow and individual-node factories, normalizing spreads to arrays only at the MessagePack transport boundary.
- [x] Bind structured NodeTool types through the C# type registry where a generated type exists.
- [x] Map image inputs/outputs to `SKImage` and document ownership/disposal rules.
- [x] Map audio, video, document, and generic asset pins to typed SDK asset references.
- [x] Use an explicit JSON/object fallback pin for unsupported types instead of silently pretending they are strings.
- [x] Surface per-workflow diagnostics when a pin uses a fallback type.
- [x] Read current native/Python node metadata flags and recursively preserve list element and binary types for individual NodeTool node pins; use an object fallback for unsupported structured node types.
- [x] Expose a consistent standard execution surface (`Trigger`, `Cancel`, `AutoRun`, status, error).
- [ ] Add tests that load representative v1 `.vl` patches and verify node/pin resolution.
- [x] Align `VL.Core` versions between the C# project and nuspec before publishing.

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
- [x] Unwrap singleton terminal value arrays for scalar integer, float, boolean, enum, and string pins without flattening spread outputs.
- [x] Preserve the latest valid media value if a terminal result contains only a URI/reference and no inline bytes.
- [x] Resolve image outputs by the derived pin type even when generic output nodes report `output_type: any`; decode inline/base64/data-URI values and fetch materialized storage URLs asynchronously.
- [x] Resolve current `asset://<stored-file>` image outputs through `/api/storage`; resolve ID-only asset references through the connected asset RPC before downloading.
- [x] Latch workflow outputs and reapply them on every VL update so scalar value-type outputs do not reset one frame after an execution event.
- [x] Use the workflow interface type when encoding every input, including graph-derived image/audio/video/document inputs.
- [x] Prefer asset/reference transport for large media; set and test explicit inline payload limits.
- [x] Make execution timeout configurable globally and per node.
- [x] Support cancellation while server-queued; client-generated job IDs allow immediate, exactly-once cancellation of queued and running jobs.
- [x] Add reconnect/replay behavior using `reconnect_job` for interrupted sockets.
- [x] Never invoke user/VL completion callbacks while holding an execution-session lock.
- [x] Marshal pin/state changes through the correct VL update context.
- [ ] Separate recoverable transport errors from workflow/node failures in diagnostics.

### Phase 6 acceptance gate

- [x] A workflow completing immediately after `run_job` still populates all outputs.
- [x] Append and replace streams produce the expected final VL value.
- [x] Terminal reconciliation fills outputs when one or more live updates were missed.
- [x] Concurrent runs do not cross-route updates.
- [ ] Cancellation and reconnect tests leave no permanently running VL state.
- [ ] Large image/media workflows stay below the documented memory and MessagePack payload bounds.

## Phase 7 — End-to-end validation and rollout

**Outcome:** The change is deployable incrementally and reversible without breaking other NodeTool clients.

- [x] Upgrade the SDK and generated-types projects from the vulnerable `MessagePack` 3.0.300 dependency to 3.1.8, align the VL nuspec dependency, and guard the change with standard-nil, binary-media, unknown-field, request-envelope, and typed-message protocol tests.

- [x] Add a hermetic backend integration harness that constructs the production REST route plugin, tRPC router, and WebSocket runner with the feature flag both off and on.
- [x] Test the required workflow-interface v1 contract over REST and correlated WebSocket RPC in CI.
- [ ] Add vvvv smoke patches for discovery, primitive workflow execution, streaming text, image input/output, cancellation, and refresh.
- [ ] Run NodeTool protocol, websocket, workflow, web, and Electron test suites affected by the additive backend route/command.
- [x] Run SDK unit tests, C# builds, VL builds, package creation, and package-content verification.
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
- [x] No required work depends on stored `input_schema` or `output_schema` being populated.
- [x] The server-derived interface is the sole authoritative workflow I/O contract used by the SDK.

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

- [x] Do not migrate or rewrite stored workflow graphs.
- [x] Do not require existing workflows to save generated schemas.
- [x] Do not make the new endpoint mandatory for non-SDK clients running workflows.
- [x] Do not redesign the NodeTool execution kernel as part of the SDK repair.
- [x] Do not generate arbitrary CLR record/enum assemblies at runtime until the stable type-binding layer and package policy are settled.
