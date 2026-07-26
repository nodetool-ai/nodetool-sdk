# 03 - NodeTool Unity Plugin

**Status:** Deferred future track; do not begin during the VL 0.1.6 release
**Order:** 3 of 3
**Requires:** [01 - NodeTool Server and Public SDK Contract](01-nodetool-server-contract.md)
and [02 - Portable C# Base and VL SDK](02-csharp-base-vl-sdk.md)

## Outcome

Provide a Unity Package Manager plugin that discovers NodeTool workflows,
creates reusable Unity workflow assets, executes workflows in the Editor and
desktop players, and maps NodeTool values/media to Unity-native types.

Unity consumes the proven portable C# catalog, execution controller,
connection manager, preflight, asset, diagnostic, and test services. It must
not duplicate protocol, discovery-cache, execution-state, asset-URI, or retry
logic.

## Initial release boundary

- Unity 6 Editor and Windows desktop standalone player.
- Mono in Editor/player plus one IL2CPP smoke build.
- Local and authenticated remote/cloud NodeTool deployments.
- Primitive, enum, list, image, file, and generic asset pins. Audio, video,
  documents, and models initially materialize as files.
- Low-level C# API, `MonoBehaviour` runner, basic workflow picker, and
  `ScriptableObject` definitions.
- Cancellation, progress, streamed outputs, terminal reconciliation, and
  explicit asset cleanup during an active client session.

The recommended `0.1` release ends after Phase U5.

Phases U0 through U5 are a strict top-to-bottom release path. Creator tooling,
Editor chat, advanced 3D pipelines, offline hosting, macOS, and additional
platforms are explicitly deferred follow-on tracks.

A checked item labelled **Deferred** records a completed scope decision, not
an implemented feature.

## Reconciliation - 2026-07-26

- [x] The Unity plugin remains a separate future work session.
- [x] No Unity implementation is required to complete or publish the first
      C#/VL SDK release candidate.
- [ ] Begin only after the VL interactive refinement pass and an explicit
      Unity version/IL2CPP compatibility decision.

## Code review baseline - 2026-07-24

No Unity package, assembly definition, or Unity-specific source currently
exists in `nodetool-sdk`. This is intentionally a greenfield adapter built only
after the portable C# boundary is proven.

Unity object creation and mutation must happen on the Unity main thread.
Network I/O, hashing, decoding, cache work, protocol processing, and other
pure managed work may run in background tasks; results are published through a
bounded main-thread finalization queue. This separation is required for both
Editor responsiveness and runtime content generation.

Runtime use is a first-class scenario, not an Editor-only extension. A shipped
game may continuously submit jobs to local or authenticated cloud NodeTool
instances, subject initially to server admission limits, bounded Unity
main-thread work, temporary-storage limits, and explicit lifecycle cleanup.
Advanced cost/content-policy/telemetry budgets are deferred.

Relevant future integration anchors already exist on the server:

- `packages/protocol/src/agent-protocol.ts` and
  `packages/websocket/src/agent/socket-route.ts` for the optional Editor agent;
- the public workflow/job/asset profile defined by plans 01 and 02 for all
  runtime generation;
- a future signed sidecar/bundle profile, kept outside the Unity process, for
  offline desktop generation.

## Unity-only ownership

Unity owns:

- `ScriptableObject` and `MonoBehaviour` integration;
- Unity serialization and inspector bindings;
- player-loop/main-thread dispatch and lifecycle;
- `Texture2D`, `Sprite`, `AudioClip`, `Material`, GameObject, and prefab
  creation;
- AssetDatabase, importers, Addressables, Editor windows, Undo, drag/drop, and
  previews.

The portable SDK owns the underlying workflow, execution, asset, and
diagnostic behavior.

## Phase U0 - Entry gate and compatibility spike

- [ ] Confirm the required exit gates in plans 01 and 02 are complete.
- [ ] Select the minimum supported Unity 6 editor version.
- [ ] Create a minimal Unity test project.
- [ ] Package the portable SDK without requiring a separate NuGet integration
  plugin.
- [ ] Compile the portable assemblies in Unity without warnings.
- [ ] Verify generated MessagePack formatters under IL2CPP.
- [ ] Connect, discover, preflight, and run the primitives fixture in Play
  Mode.
- [ ] Build and run the same scene in a desktop IL2CPP player.
- [ ] Record the supported and deferred platform matrix.
- [x] **Deferred from the initial outcome:** detailed package/player size and
  stripping benchmarks beyond confirming that the smoke build is practical.

### Gate

- [ ] Unity Editor and IL2CPP execute one workflow through the portable SDK.
- [ ] No Unity assembly references `Nodetool.SDK.VL`.
- [ ] No Unity code reimplements a wire DTO or protocol state machine.

## Phase U1 - Package skeleton and lifecycle

- [ ] Create UPM package `ai.nodetool.unity`.
- [ ] Add Runtime, Editor, Tests, Samples, and Documentation assemblies.
- [ ] Add package metadata, license, changelog, and third-party notices.
- [ ] Implement `NodeToolConnectionProfile` as a Unity projection over portable
  connection options.
- [ ] Implement `NodeToolRuntime` and main-thread dispatcher.
- [ ] Add a bounded main-thread finalization queue while keeping network,
  protocol, hashing, cache, and safe decoding work off the main thread.
- [ ] Bind portable connection/catalog/execution services to Unity lifecycle.
- [ ] Handle play-mode exit, domain reload, application quit, scene unload,
  component disable, and destruction.
- [ ] Keep editor-only code and dependencies out of runtime assemblies.
- [ ] Add structured Unity logging with connection, request, and job IDs.
- [ ] Route Unity logs and diagnostics through the portable SDK's redaction
  policy.
- [ ] Add a minimal connection-status sample.
- [x] **Deferred from the initial outcome:** a runtime diagnostics overlay.

### Gate

- [ ] Repeated Play Mode entry/exit leaves no live socket, task, or Unity
  object.
- [ ] Domain reload enabled and disabled both work.

## Phase U2 - Workflow discovery and authoring

- [ ] Add `NodeToolWorkflowDefinition : ScriptableObject`.
- [ ] Store stable workflow ID, display information, interface etag/revision,
  pin descriptors, refresh state, and diagnostics.
- [ ] Treat the server-derived interface as authoritative.
- [ ] Build a searchable workflow browser.
- [ ] Create and refresh workflow definition assets through the portable
  catalog.
- [ ] Present a simple stale/incompatible state.
- [ ] Preserve the last valid definition when refresh fails.
- [ ] Add custom inspectors for connection profiles and workflow definitions.
- [x] **Deferred from the initial outcome:** rich interface diff UI,
  drag-and-drop/context actions, and a Connection Doctor window.

### Gate

- [ ] Discovery remains responsive for a representative project catalog.
- [ ] Refresh never corrupts an existing workflow asset.
- [ ] Stale or incompatible definitions are visibly diagnosed.

## Phase U3 - Runtime runner and bindings

- [ ] Add `NodeToolWorkflowRunner : MonoBehaviour`.
- [ ] Bind Unity inputs to portable `WorkflowInvocation` values.
- [ ] Support primitive, enum, list, color, vector, structured, and asset
  bindings.
- [ ] Expose state, progress, output, preview, completion, cancellation, and
  error events.
- [ ] Marshal portable controller snapshots onto the Unity main thread.
- [ ] Support run-on-start and manual trigger modes.
- [ ] Provide a simple authored fallback/last-known-good option.
- [ ] Respect server concurrency and request limits and bound main-thread
  finalization work.
- [ ] Support authenticated remote service configuration without storing
  secrets in project assets.
- [ ] Cancel or detach cleanly when runner/scene/application ownership ends.
- [x] **Deferred from the initial outcome:** debounced input execution,
  scheduler policy matrices, invocation preset assets, client-side cost/rate
  budgeting, content-policy integration, and telemetry hooks.

### Gate

- [ ] Primitive/list outputs retain their types and do not flash for one frame.
- [ ] Sequential and overlapping runs cannot cross-route outputs.
- [ ] Cancellation/reconnect cannot leave a runner permanently busy.

## Phase U4 - Image and file vertical slice

- [ ] Bind portable asset leases/materialization to Unity ownership.
- [ ] Upload `Texture2D` through PNG/JPEG and support file/URL/reference inputs.
- [ ] Materialize image outputs as `Texture2D` and optionally `Sprite`.
- [ ] Materialize audio, video, document, font, model, text, and generic assets
  as local files.
- [ ] Support file output, project-asset import, and direct image assignment.
- [ ] Use bounded temporary storage and per-frame finalization limits.
- [ ] Add sample scenes for primitives, image roundtrip, cancellation, and
  runtime UI.
- [x] **Deferred from the initial outcome:** `AudioClip`/`VideoPlayer`
  conversion, broad component/material destinations, cache/history inspector,
  sample-scene generation commands, and recovery across app suspension or
  process restart.

### Gate

- [ ] Image roundtrip works in Editor and IL2CPP.
- [ ] Large media travels by reference rather than inline frames.
- [ ] A bounded repeated-generation smoke test does not leak Unity objects,
  sockets, or temporary files.

## Phase U5 - Minimum productization and `0.1`

- [ ] Add focused Edit Mode tests for definitions and value/asset mappings.
- [ ] Add focused Play Mode tests for lifecycle, execution, cancellation, and
  image/file materialization.
- [ ] Add one live-backend smoke test using the small SDK workflows.
- [ ] Add CI compilation plus one Windows desktop/IL2CPP smoke job.
- [ ] Publish an internal prerelease.
- [ ] Document compatible protocol, C# SDK, backend, Unity version,
  installation, authentication, lifecycle, and known limitations.
- [ ] Collect feedback before stabilizing the runtime API.

### Exit gate

- [ ] The Unity package contains only host-specific behavior.
- [ ] Shared protocol and execution scenarios match console and VL behavior.
- [ ] Unity Editor and a Windows desktop player can discover, run, cancel, and
  materialize primitive/image/file outputs reliably.

## Deferred follow-on tracks

The following are useful product directions, but none blocks the initial
server → C# → VL → Unity outcome:

- [x] **Deferred:** creator history, provenance UI, candidate comparison,
  batch generation, seed/preset UX, sprite sheets, animation clips, tiles,
  flipbooks, material texture sets, and audio import helpers.
- [x] **Deferred:** Unity batch-mode generation/import and CI content-foundry
  orchestration.
- [x] **Deferred:** the optional Editor agent/chat, portable agent client,
  Unity context tools, approval UI, and mutation audit history. NodeTool's
  existing `/ws/agent` foundation remains the future integration point.
- [x] **Deferred:** constrained glTF/GLB import, validation, prefab/collider/LOD
  generation, Addressables promotion, world building, and gameplay-data
  modules.
- [x] **Deferred:** the offline desktop sidecar, signed workflow/model bundles,
  local model management, sandboxing, resource budgets, installer/update, and
  licensing work.
- [x] **Deferred:** macOS, WebGL, mobile, console, and broader IL2CPP platform
  certification.
- [x] **Deferred:** generation history/cache inspectors, multi-hour soak
  automation, expiring-asset recovery, and server-restart job recovery.

## Explicitly out of scope

- [x] In-process Python/model hosting inside Unity.
- [x] Arbitrary runtime 3D formats.
- [x] Automatic strongly typed workflow wrapper generation.
- [x] Direct video encoding.
- [x] Arbitrary shader/code generation.
- [x] Gameplay/runtime agent chat.
- [x] Autonomous Editor mutations without approval and audit history.
