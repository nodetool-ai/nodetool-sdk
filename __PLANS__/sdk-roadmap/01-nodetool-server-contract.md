# 01 - NodeTool Server and Public SDK Contract

**Status:** Initial active-session SDK profile implemented; interactive
non-regression and follow-on hardening remain
**Order:** 1 of 3
**Next:** [02 - Portable C# Base and VL SDK](02-csharp-base-vl-sdk.md)

## Outcome

NodeTool exposes one versioned, language-neutral SDK contract that lets a
client safely:

1. discover capabilities and workflows;
2. determine whether a workflow can run;
3. submit a workflow through the current job-scoped execution path;
4. observe and cancel an active job and handle disconnect deterministically;
5. receive authoritative terminal outputs and referenced assets during the
   active client session.

The TypeScript backend is the authoritative implementation. C#, VL, Unity, and
future language SDKs consume the contract instead of inferring backend
behavior.

This plan targets the current TypeScript server. Compatibility with retired
REST behavior or older NodeTool server versions is not a requirement. During
the transition, existing current clients must remain usable until they move to
the new operations; compatibility shims should then be removed deliberately.

## Current-backend non-regression policy

The SDK work must not break the current NodeTool web app, Electron app, CLI,
agent routes, workflow execution, or existing asset behavior.

- New SDK operations begin as additive routes/commands over existing services,
  not as a second workflow runner.
- New behavior that changes execution, persistence, discovery, or result
  semantics is initially guarded by a narrowly scoped server feature flag.
- The flag defaults must preserve the current product behavior until the new
  path has passed backend, C#, VL, and current-client smoke tests.
- Database migrations must preserve current job, workflow, and asset reads and
  must be safe for the normal NodeTool upgrade path.
- Existing `run_job` and current discovery routes remain operational while
  clients migrate to the public protocol profile.
- Shared validation, queueing, execution, cancellation, and asset services are
  extended in place; public SDK endpoints must not fork their business logic.
- Removal or semantic replacement of a current route requires an explicit
  migration task, release note, and current-client cutover test.

This protects the current backend and current clients. It does not require the
new SDK to support old NodeTool server releases.

## Execution rule

Phases 0 through 6 are implemented in order. A later phase may be researched
or prototyped, but it must not become the production path until the preceding
gate passes. Checklist order within each phase is the recommended build order.

A checked item labelled **Deferred** records a completed scope decision, not an
implemented feature. Deferred items do not block the initial exit gate.

## Reconciled first-release boundary - 2026-07-26

- [x] Workflow-interface v1, capability discovery, authoritative preflight,
      active-session execution/cancellation, referenced assets, and optional
      low-overhead execution settings are implemented through the current
      runner.
- [x] The additive workflow-interface and lifecycle profiles are default-on
      with server-owned emergency kill switches; normal NodeTool startup needs
      no SDK enable flags.
- [x] Focused protocol, transport, authorization, preflight, runner, and
      default/kill-switch regression suites pass.
- [ ] Run the remaining Electron/current-client and packed-VL interactive
      non-regression matrix.
- [x] **Deferred beyond the first release:** durable terminal-job recovery,
      exhaustive phase timings/benchmarks, full asset retention/provenance,
      cross-language golden scenario runners, and multi-instance semantics.

Unchecked detail below those deferred themes is retained as follow-up design
material and does not block the initial C#/VL release candidate.

## Code review baseline - 2026-07-24

The longer repository review found more working server infrastructure than the
original plan assumed:

- `@nodetool-ai/protocol` already owns shared graph, message, API, asset, job,
  and runtime-validating Zod types, although public SDK coverage is incomplete.
- Workflow interface v1 is derived from the hybrid TypeScript/Python node
  registry and is exposed consistently through REST, tRPC, and correlated
  WebSocket RPC.
- `/ws` accepts JSON text and MessagePack commands. Outbound mode defaults to
  interoperable MessagePack and can be changed with `set_mode`.
- `validateGraph` and runner node validation already provide the static
  validation foundation; `estimateWorkflowCost` already provides the cost
  estimation foundation.
- The runner already has in-memory global/per-workflow admission queues,
  persisted jobs, cancellation, reconnect, suspension/resume, heartbeats, and
  recovery-oriented job fields.
- Active reconnect sends status and graph snapshots while the owning runner is
  alive. Once that runner is lost, reconnect fails explicitly because final
  outputs and ordered events are not persisted; it never fabricates an empty
  successful result.
- Assets already have Zod schemas, multipart REST upload, storage download,
  signed URL support, thumbnails, and job/workflow provenance fields. The
  public durability, checksum, retention, and result-manifest semantics remain
  incomplete.

These findings change the implementation strategy from replacement to
promotion: expose, version, and test the existing primitives behind a small
public SDK profile, adding persistence only where the initial outcome needs it.

Review verification:

- [x] Node SDK workflow-interface, graph-validation, and cost-estimation tests
      pass (30 tests on 2026-07-24).
- [x] WebSocket workflow-interface parity, read-only RPC, and run-job coverage
      tests pass (41 tests on 2026-07-24).

Primary implementation anchors in the sibling `nodetool` repository:

- `packages/protocol/src/messages.ts` and `packages/protocol/src/api-schemas/`
  for the current wire and runtime schemas;
- `packages/node-sdk/src/workflow-interface.ts`,
  `graph-validation.ts`, and `cost-estimate.ts` for derivation and preflight
  foundations;
- `packages/websocket/src/workflow-interface-service.ts`,
  `http-api.ts`, and `trpc/routers/workflows.ts` for discovery surfaces;
- `packages/websocket/src/unified-websocket-runner.ts` for submission,
  queueing, streaming, cancellation, reconnect, and current result loss;
- `packages/models/src/job.ts` and `packages/models/src/schema/jobs.ts` for
  durable job state;
- `packages/protocol/src/api-schemas/assets.ts`,
  `packages/websocket/src/trpc/routers/assets.ts`, and
  `packages/websocket/src/storage-api.ts` for asset foundations.

## Scope boundary

NodeTool owns:

- protocol version, capabilities, limits, and requirement reporting;
- authoritative workflow interfaces and preflight;
- admission, queueing, and job identity;
- active-session event routing, cancellation, and terminal results;
- canonical asset references and current cost reporting.

Durable completed-job recovery, ordered replay, rich provenance/history, and
multi-instance semantics are follow-on profiles, not initial release gates.

Clients may provide faster local feedback, local scheduling, and local caches,
but they must not replace server authority.

Internal Python-worker messages, database models, editor state, and provider
implementation details are not part of the public SDK contract.

Acknowledged idempotent submission remains represented in the wider protocol
design, but is deferred from the first client release. The initial clients use
the existing job-scoped execution command and do not automatically retry an
ambiguous submission.

## Phase 0 - Preserve the working foundation

- [x] Provide graph-derived workflow interface v1.
- [x] Provide compact workflow summaries and bounded bulk interface retrieval.
- [x] Provide correlated read-only WebSocket RPC responses.
- [x] Normalize public MessagePack frames for non-JavaScript clients.
- [x] Provide graph/per-node validation and structured validation issues.
- [x] Provide global/per-workflow concurrency limits and queue positions.
- [x] Persist job rows and provide reconnect, resume, and cancel foundations.
- [x] Track provider costs and support estimate/actual reconciliation internally.
- [x] Verify REST, tRPC, JSON-text WebSocket, and MessagePack workflow-interface
      parity in the existing integration tests.
- [x] Verify static graph validation and cost-estimation unit coverage.
- [x] Add optional per-output `stream_kind` metadata (`text`, `audio`,
      `control`, `image`, `video`, `document`, or `binary`) and propagate it
      through TypeScript node metadata, Python bridge metadata, graph-derived
      workflow interfaces, and generated discovery schemas. Keep it
      descriptive: no runner, persistence, or ordinary output-routing changes.
- [x] Audit reconnect and confirm that completed persisted jobs currently lose
      their terminal output map.
- [x] Capture the current successful REST, JSON-text WebSocket, and MessagePack
      payloads as baseline fixtures.
- [x] Record the default state and owner of every SDK-related server feature
      flag.
- [x] Centralize SDK feature-flag evaluation, require exact `1` opt-in, and
      test workflow, node, asset, and execution behavior with all flags disabled.
- [x] Keep existing focused backend protocol, workflow-interface, runner, and
      authorization tests green throughout the work.

### Gate

- [ ] Current NodeTool web, Electron, CLI, and C# clients still execute the
      existing small workflow fixtures.
- [x] Automated default/kill-switch coverage preserves current routes,
      execution, database, and asset behavior; the remaining Electron/VL
      checks are tracked by the preceding interactive gate.

Automated results, known Windows/sandbox baseline failures, and the remaining
interactive checks are recorded in the sibling NodeTool repository at
`docs/sdk/non-regression-baseline-2026-07-24.md`.

Live verification on 2026-07-24 now covers the Vite web client and C#
TestConsole discovery/execution. The aggregate gate remains open for the
Electron shell, VL media smoke, and flags-disabled pass.

## Phase 1 - Publish public protocol v1

- [x] Classify existing `@nodetool-ai/protocol` types as public SDK, optional
      profile, or internal.
- [x] Convert public TypeScript-only wire interfaces into runtime-validating
      schemas.
- [x] Define JSON Schema 2020-12 components for workflows, commands, events,
      jobs, assets, errors, requirements, and capabilities.
- [x] Publish OpenAPI for public SDK HTTP request/response operations.
- [x] Publish AsyncAPI for public SDK WebSocket commands and server events.
- [x] Specify JSON text and MessagePack binary framing over WebSocket,
      including MessagePack as the default outbound mode and `set_mode`
      negotiation.
- [x] Specify correlation IDs, job IDs, client request IDs, timestamps, and
      unknown-field/event behavior.
- [x] Specify authentication, authorization, tenant/workspace ownership, and
      not-found behavior that does not leak another user's resources.
- [x] Define one structured public error envelope with stable codes, safe
      details, retryability, and redaction rules.
- [x] Document execution lifecycle, terminal states, ordering, cancellation,
      retry, reconnect, and asset ownership semantics.
- [x] Add a protocol manifest containing public protocol version, artifact
      hashes, and optional profiles.
- [x] Generate the protocol bundle deterministically from
      `@nodetool-ai/protocol`.
- [x] Fail CI when generated protocol artifacts differ from committed output.

### Gate

- [x] A developer can understand the public client protocol without reading
      the WebSocket runner or C# implementation.
- [x] The protocol bundle validates all captured baseline fixtures.

## Phase 2 - Capabilities and authoritative preflight

- [x] Add flag-gated HTTP `get_capabilities` using the schema-validating,
      transport-independent response builder and live runtime provider.
- [x] Compose capability snapshots from live registry/Python/profile state and
      the storage layer's enforced upload limit.
- [x] Register the redacting HTTP capabilities adapter behind the default-off
      lifecycle flag; flag-off requests cannot evaluate runtime state.
- [x] Add a standalone, schema-validating WebSocket response adapter for
      capabilities and preflight, with HTTP payload parity and redacted
      failures; keep it out of the runner until the Phase 0 gate passes.
- [x] Return public protocol version, NodeTool version, supported encodings,
      optional profiles, registry revision, and Python bridge readiness.
- [x] Return documented limits for RPC batches, inline payloads, uploads,
      queueing, timeouts, and retained job events.
- [x] Return supported asset URI schemes and authentication requirements.
- [x] Add side-effect-free HTTP `preflight_workflow` behind the default-off
      lifecycle flag and the existing authenticated server boundary.
- [x] Add an HTTP preflight adapter with request validation,
      authenticated-principal injection, stable service errors, and redacted
      unexpected failures, and register it only after the Phase 0 gate passes.
- [x] Support `static`, `availability`, and `execution` preflight levels.
- [x] Implement transport-neutral static, availability, and execution
      preflight composition without registering a production route.
- [x] Bind workflow authorization and per-user requirement lookup to the same
      authenticated principal.
- [x] Reuse `validateGraph` and the runner's existing per-node validation for
      the static level instead of introducing a second validator.
- [x] Validate workflow access, etag/interface version, inputs, graph, nodes,
      pins, and types.
- [x] Report missing node packages, providers, credentials, models, model
      downloads, runtimes, and assets.
- [x] Preserve exact node-package provenance in the shared registry for
      Python metadata, built-in registrars, and trusted third-party packs;
      derive package requirements only from that provenance, never from a
      namespace.
- [x] Resolve node-package availability against the registry's exact,
      read-only installed-package inventory with principal-bound injection
      and bounded failure handling.
- [x] Expose immutable Hugging Face download-manager snapshots and map
      already-active downloads into `downloading` through an explicitly
      provider-owned, read-only adapter. Do not begin, retry, or cancel a
      download during preflight.
- [x] Report worker/target readiness, capacity, likely queueing, and known
      resource requirements.
- [x] Add a persisted-worker readiness adapter that accepts only a local
      `listWorkers` authority and treats `running`/`attached` as ready. It does
      not call provider status, reconcile, attach, resume, or provision.
- [x] Probe local Node, hydrated Python, and allow-listed FFmpeg/FFprobe
      runtime readiness without installing software or starting work.
- [x] Expose the runner's admission counters through a read-only capacity
      snapshot and report likely queueing as a non-blocking warning.
- [x] Add an optional additive execution-target selector for local execution
      or one explicit worker ID; omitted requests remain local and an explicit
      worker never falls back to a different attached worker.
- [x] Wire production execution preflight to side-effect-free local/attached
      worker readiness and report runner capacity as unknown when no live
      runner identity is selected.
- [x] Issue an opaque live runner target identity on WebSocket connect, scope
      it to the authenticated user, unregister it on disconnect, and let HTTP
      preflight read only that exact runner's admission snapshot.
- [x] Preserve each selected model's provider context in preflight
      requirements so an authoritative inventory probe can resolve the correct
      provider without guessing.
- [x] Discover asset requirements from structured asset values and canonical
      asset URI references without loading their media payloads.
- [x] Preserve model type and provider context while deriving requirements,
      then check the selected model through a bounded, provider-scoped,
      injected cache/local inventory without guessing providers, contacting a
      remote model-list endpoint, or starting a download.
- [x] Compose cached model inventory from explicitly associated local sources
      and the Python bridge's cache-only `models.list_cached` operation;
      normalize model IDs/repository IDs and report unavailable sources as
      unknown rather than absent.
- [x] Return blocking errors and non-blocking warnings with stable codes.
- [x] Reuse `estimateWorkflowCost` for the cost portion and define how stale or
      unavailable provider pricing affects confidence.
- [x] Return cost estimates, unknown-cost nodes, currency, confidence, and
      approval metadata without starting paid work.
- [x] Populate workflow `required_providers`, `required_models`, packages, and
      compatible execution targets instead of placeholder nulls. Keep compact
      workflow summaries graph-free; publish graph-derived requirements
      through the interface/preflight profile rather than loading large
      workflow JSON in list queries.

The registered HTTP preflight service implements static
graph/interface/input
validation, deterministic requirement discovery, cost mapping, injected
read-only availability probes, and execution-readiness composition. A
transport-neutral orchestrator loads workflows through an adapter over
NodeTool's existing owner/public/collaborator authorization and graph-derived
interface service. Credential, registered-provider, owner-scoped asset,
Node/Python/FFmpeg runtime, exact node-package, and runner-capacity checks
reuse normal NodeTool state. Selected models can be checked only against their
recorded, configured provider and model type through an injected cache/local
inventory; preflight never performs an implicit remote provider listing. The
cache inventory adapter supports explicit TypeScript/local sources and the
Python bridge's cache-only listing. Existing Hugging Face download state is
wired to the exact `huggingface` provider without creating manager state.
Persisted remote-worker state has a read-only adapter. The production HTTP
route now exposes execution preflight with additive local, explicit-worker, or
live-runner selection. It checks only the exact attached worker requested and
performs no attach/provision action. Each WebSocket connection receives an
opaque runner ID; an authenticated HTTP preflight using that ID reads only the
same user's matching runner admission snapshot. Local/worker requests without
a runner ID keep capacity as a non-blocking `unknown` rather than borrowing
another connection's queue.

### Gate

- [x] The same preflight request returns logically equivalent results over
      supported HTTP and WebSocket surfaces.
- [x] Preflight never creates a job or provider request.

## Phase 3 - Authoritative active-session terminal results

- [x] **Deferred from the initial outcome:** persisted completed-job snapshots,
      polling-only clients, and reconnecting to terminal jobs after a server
      restart. The initial SDK may rely on authoritative terminal output from
      the active WebSocket session.
- [x] Preserve authoritative terminal status, final public outputs, errors,
      and costs in the active job/session result.
- [x] Emit completion only after final public outputs are available.
- [x] **Deferred from the initial outcome:** persist a complete result manifest,
      add polling snapshots, and define expired-snapshot behavior.

### Gate

- [x] Active jobs return authoritative terminal outputs without one-frame or
      live-update/final-output disagreement.
- [x] No terminal job can be observed before its authoritative result exists.

## Phase 4 - Promote the current execution and cancellation path

- [x] Document the existing job-scoped `run_job` command as the initial SDK
      execution operation rather than introducing a second runner.
- [x] Return or confirm the job ID before routing job-scoped updates.
- [x] Preserve structured queue/rejection responses and authoritative terminal
      output reconciliation.
- [x] Reject workflow etag/interface mismatches during authoritative preflight
      before provider work begins.
- [x] Enforce the same owner/workspace authorization for submit, cancel, and
      result assets.
- [x] Keep cancellation job-scoped and deterministic for queued and running
      jobs.
- [x] Document that clients must not automatically retry an ambiguous
      submission in the initial profile.
- [x] **Deferred from the initial outcome:** acknowledged `submit_job`,
      idempotency keys, duplicate-key recovery, and lost-acknowledgement retry.
- [x] **Deferred from the initial outcome:** durable queues, multi-instance or
      server-restart recovery, ordered event replay, exhaustive idempotency
      retention/expiry rules, and ambiguous external-provider reconciliation.
- [x] **Deferred from the initial outcome:** fine-grained per-workspace
      admission diagnostics beyond the existing structured queue/rejection
      responses.

### Gate

- [x] C#, VL, and the current web client use the same underlying runner and
      job-scoped update semantics.
- [x] Queue and rejection responses are stable, structured, and testable.
- [x] An active client can cancel and receive one authoritative terminal
      outcome; server-restart recovery is not an initial release gate.

## Phase 4A - Optional low-overhead SDK execution profile

This is a bounded optimization phase, not a second runner. Existing Electron,
WebSocket, HTTP, and unannotated `run_job` behavior remains unchanged.
Capabilities declare which relaxations the server honors. Public SDK runs
default generated-asset autosave to off; callers explicitly select `auto` when
they want persistent generated assets.

- [x] Enable the additive SDK workflow-interface and lifecycle v1 profiles by
      default after their current regression suites pass.
- [x] Retain explicit server-side kill switches for those profiles; clients
      may select supported behavior but cannot enable disabled server
      functionality.
- [x] Keep SDK authentication, authorization, limits, persistence policy, and
      other deployment-wide controls exclusively server-owned.
- [x] Ensure the SDK discovery exemption can never bypass an authenticated
      server; the SDK auth flag may only tighten local-mode policy.

Short path review (2026-07-25):

- Graph lookup, graph hydration, validation, scheduling, terminal outputs,
  cancellation, errors, and authorization are execution-critical and must not
  be skipped.
- Job-row create/update/final-save performs several SQLite operations and is
  optional for an active-session-only SDK run that does not need queue/history
  recovery.
- Per-node and per-edge status relay, normalization, and WebSocket traffic are
  optional when a client only consumes public output updates or the terminal
  result. Error and terminal messages are never suppressible.
- Generation autosave may perform asset queries, media encoding, thumbnail
  generation, storage writes, and database writes. It is optional only when
  the caller accepts temporary/non-durable output references.
- The current runner emits only a few structured start/end/error log records;
  adding a log-disable flag is not justified unless profiling shows material
  cost. Error logs remain enabled.

- [x] Add a versioned, additive per-run `execution_options` object with:
      `persistence = "job" | "session"`,
      `event_detail = "full" | "outputs" | "terminal"`, and
      `asset_persistence = "auto" | "temporary"`.
- [x] Keep ordinary/unannotated server defaults at `job`, `full`, and `auto`;
      SDK runs default asset persistence to `temporary`, while an explicit
      `auto` remains available.
- [x] Advertise supported values in SDK capabilities before a client sends
      non-default options.
- [x] For session persistence, skip queued/running/final `Job` database writes
      while retaining active in-memory status, queueing, cancellation, costs,
      and the authoritative terminal result.
- [x] For output-only events, omit ordinary node/edge progress messages while
      retaining public `output_update`, errors, queue state, cancellation, and
      the terminal result.
- [x] For terminal-only events, omit provisional progress/output events and
      return one authoritative terminal result with normalized public outputs.
- [x] For temporary assets, bypass generation-history autosave and thumbnail
      work while preserving output materialization needed by the requesting
      client.
- [x] Add capability-advertised
      `POST /api/sdk/v1/assets/temporary` for large SDK execution inputs;
      write directly to temporary storage without an Asset row, thumbnail, or
      asset-list entry, and return a runtime-resolvable URI.
- [x] **Deferred timing refinement:** add phase timing around graph lookup/hydration, pre-run readiness,
      persistence, kernel execution, event/output normalization, autosave, and
      finalization. Timings are diagnostic and do not include inputs or secrets.
      Graph lookup, hydration, readiness, initial persistence, queue delay,
      execution/relay, and total time are implemented; separate normalization,
      autosave, and finalization counters remain.
- [x] **Deferred benchmark:** benchmark primitives without media, repeated image roundtrip, one
      generated image, and one audio output under the default and each
      low-overhead option.
- [x] **Deferred policy refinement:** reject incompatible combinations explicitly, including session-only
      persistence when a run is expected to survive server restart and
      temporary assets when durable outputs are requested.

### Gate

- [x] Focused server tests prove default executions remain behaviorally
      identical; the Electron interactive pass remains in Phase 0.
- [x] Each advertised option has an isolated test proving which work is
      skipped and which terminal/error/cancellation guarantees remain.
- [x] Select only the low-risk SDK default `asset_persistence = temporary`;
      keep persistence and event-detail defaults unchanged until benchmark
      follow-up.

## Phase 5 - Canonical asset references and minimum result safety

- [x] Provide current asset records with IDs, MIME type, optional size,
      metadata, ownership, download URL, and workflow/node/job associations.
- [x] Provide multipart asset upload, storage download, and signed URL
      foundations.
- [x] **Deferred asset-contract refinement:** define asset IDs, MIME type, size, checksum, metadata, ownership, access,
      and expiry in the public contract.
- [x] Use current multipart upload for protocol v1 and advertise its enforced
      size limit through capabilities.
- [x] Publish validation, authentication, and size limits for the initial
      upload
      path.
- [x] **Deferred durability refinement:** convert temporary provider URLs into durable NodeTool assets when the
      workflow output contract requires durability.
- [x] Ensure large media travels by reference rather than inline WebSocket
      payload.
- [x] Never persist credentials, authorization headers, provider secrets, or
      unbounded inline media in job provenance.
- [x] **Deferred from the initial outcome:** resumable upload sessions, signed
      URL expiry, configurable retention/pinning, rich lineage/provenance,
      sensitive-content propagation rules, shared history, candidate batches,
      and offline workflow/model bundles.

### Gate

- [x] **Deferred independent-client conformance:** verify the same output asset
      through CLI, C#, VL, and a second independent client.

## Phase 6 - Cross-language conformance and release

- [x] Provide current server tests for transport parity, authorization,
      interface batching, MessagePack interoperability, terminal streaming,
      queueing, cancellation, and cost persistence.
- [x] Publish a minimum generated JSON baseline for discovery and protocol
      artifacts; broader golden execution scenarios are deferred.
- [x] **Deferred conformance expansion:** publish a small golden JSON fixture set for discovery, preflight,
      submission, completion, cancellation, and asset references.
- [x] **Deferred conformance expansion:** validate the NodeTool server and C# SDK against that fixture set.
- [x] Add cross-tenant authorization, resource-existence leak, and secret
      redaction scenarios.
- [x] Document additive capabilities, kill switches, and initial compatibility
      policy; formal deprecation cadence remains follow-up release policy.
- [x] Publish protocol v1 reference documentation and a minimal walkthrough.
- [x] **Deferred from the initial outcome:** exact MessagePack byte fixtures,
      full scenario transcripts, a language-independent conformance runner,
      an additional independent client, and compatibility-result release
      artifacts.

### Exit gate

- [x] Protocol v1 is the documented source of truth for all SDK clients.
- [x] **Deferred cross-language conformance:** server and C# pass the same
      expanded JSON scenario fixtures.
- [x] Discovery, preflight, submission, cancellation, terminal results, and
      referenced assets are reliable for an active client session.

## Explicitly deferred

- [x] Internal Python bridge standardization.
- [x] Protobuf or a second wire format without measured need.
- [x] Full database/admin API specification.
- [x] Agent protocol beyond a separately versioned optional profile.
- [x] Cross-client candidate boards before reliable results/provenance.
