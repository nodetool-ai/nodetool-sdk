# Model Catalog, Dynamic Model Pins, and HDE Model Manager

**Status:** In progress
**Order:** server contract -> portable C# -> VL pins -> optional HDE
**Scope:** NodeTool server, `Nodetool.SDK`, `Nodetool.SDK.VL`, and
`VL.Nodetool.HDE`

## Outcome

Model-valued node and workflow inputs no longer appear as `System.Object` when
NodeTool declares a compatible model type. vvvv presents a live, typed model
selection whose entries reflect the connected NodeTool server. An optional HDE
window shows ready and recommended models and can start, monitor, cancel, and
retry downloads.

The portable C# SDK owns the reusable model catalog and download operations.
The VL adapter owns dynamic enum projection. The HDE owns only editor UI and is
never required for discovery, execution, or exported applications.

The HDE uses a deliberately thin `.HDE.vl` bootstrap for editor command,
docking, and window lifecycle. Its state and actions live in C#. The initial UI
is composed with the vvvv-shipped ImGui/Skia nodes rather than implementing a
new widget toolkit in C#.

## Constraints

- [x] Keep the current NodeTool web, Electron, tRPC, and execution behavior
      unchanged; SDK support is additive.
- [x] Treat the connected NodeTool server as authoritative, including when it
      is a remote worker or cloud instance.
- [x] Distinguish locally installed models from configured remote/provider
      models; both may be ready for execution.
- [x] Keep model catalog, selection, and download behavior in portable C# so
      future C# clients can reuse it.
- [x] Keep VL dynamic enums and HDE/editor APIs out of the portable SDK.
- [x] Do not require the HDE for runtime or exported-patch operation.
- [x] Do not infer model compatibility from a pin name when authoritative type
      metadata is unavailable.
- [x] Keep HDE startup lazy: loading the package must not connect, discover, or
      allocate model UI state until the window is opened.
- [x] Account for the separate HDE/editor runtime; share connection settings
      and portable implementations, but do not assume the user patch's live
      execution socket is directly accessible.

## Phase 0 - Characterize the current model system

- [x] Confirm that node metadata already identifies model inputs with types
      such as `language_model`, `image_model`, `llama_model`, `hf.*`, and
      `tjs.*`.
- [x] Confirm why model pins currently become `System.Object`: the VL mapper
      only promotes finite option lists to dynamic enums and has no live model
      catalog projection.
- [x] Inventory the existing NodeTool model APIs for provider models,
      recommended models, local and worker Hugging Face caches, and compatible
      model-type filtering.
- [x] Confirm that NodeTool already exposes download commands, cancellation,
      and progress events that can back an SDK abstraction.
- [x] Confirm that the existing VL dynamic enum implementation can map a
      display entry to an arbitrary structured wire value.
- [x] Capture compact fixtures for a provider model, provider-locked model,
      `llama_model`, `hf.*`, `tjs.*`, and an unknown/unclassified cached model.
- [x] Record the exact wire value expected by each representative model input.

### Gate

- [x] Compatibility and serialization rules are covered by fixtures before
      changing generated VL pin types.

## Phase 1 - Add an SDK-facing server model contract

- [x] Define a versioned, normalized model catalog DTO in the shared protocol.
- [x] Include stable model identity, display name, declared model type,
      provider, repository/path data, supported tasks, size, and recommendation
      state where known.
- [x] Define explicit availability states such as `ready_local`,
      `ready_remote`, `downloadable`, `downloading`, and `unavailable`.
- [x] Include an authoritative compatibility key suitable for filtering a
      specific node or workflow input.
- [x] Include catalog revision and execution scope (`local` or worker/remote)
      so clients can cache and refresh safely.
- [x] Add a bounded SDK catalog query with filters for compatibility,
      availability, provider, and scope.
- [x] Implement the SDK endpoint over existing model services rather than
      duplicating provider and Hugging Face classification logic.
- [x] Add a versioned local download operation and progress contract that
      reuses the existing Hugging Face and Transformers.js downloaders.
- [x] Extend the same contract to the existing worker catalog and download
      relay, with explicit unavailable errors for absent or older workers.
- [x] Support start, progress, completion, failure, cancellation, and retry
      without tying the public SDK contract to the current web UI transport.
- [x] Provide a current download-state snapshot so a newly opened or restarted
      HDE can recover in-progress downloads before subscribing to new events.
- [x] Advertise model-catalog and model-download capabilities through existing
      SDK capability negotiation.
- [x] Keep existing tRPC, web, Electron, and execution call sites unchanged.
- [x] Add authorization-policy, local/worker scope, unavailable-worker,
      bounded-response, relay, and cancellation tests.

### Gate

- [ ] A non-web client can list compatible ready models and monitor one model
      download using only the public SDK contract.
- [ ] Enabling the additive SDK surface does not change existing NodeTool model
      or execution behavior.

## Phase 2 - Implement the portable C# model layer

- [x] Add immutable model descriptor, compatibility, availability, catalog
      snapshot, and download-progress types to `Nodetool.SDK`.
- [x] Add `IModelCatalog` with filtered lookup, refresh, revision, last-known-
      good snapshot, and structured diagnostics.
- [x] Scope catalog caches by endpoint, authenticated user, and execution
      target so local and cloud inventories cannot mix.
- [x] Preserve the last successful catalog during transient connection or
      refresh failures.
- [x] Add a model-selection value that preserves the server's complete wire
      object instead of reducing it to a display string.
- [x] Add checked conversion between catalog selections and node/workflow input
      values.
- [x] Add `IModelDownloadService` with start, cancel, retry, and observable
      progress/state updates.
- [x] Refresh the affected catalog scope after a successful download.
- [x] Keep the implementation free of VL, HDE, Skia, Stride, and Unity
      dependencies.
- [x] Add tests for cache scoping, refresh failure, structured wire values,
      progress ordering, cancellation, and reconnect behavior.

### Gate

- [ ] A plain C# console client can list compatible models, select one, start a
      download, display progress, and execute with the selected wire value.

## Phase 3 - Project compatible models as live VL enums

- [x] Recognize authoritative model types in individual-node and workflow pin
      metadata before falling back to `System.Object`.
- [x] Add a dedicated dynamic model enum factory rather than coupling mutable
      model catalogs to finite workflow option enums.
- [x] Give each dynamic model enum a stable CLR identity based on its
      compatibility signature, not on the current list of installed models.
- [x] Update enum entries in place when the catalog revision changes so model
      installation does not require restarting vvvv or rebuilding unrelated
      node descriptions.
- [x] Use readable, collision-safe labels that include provider or source only
      when needed.
- [x] Map each enum value to the exact structured model wire object expected by
      NodeTool.
- [x] Show only ready compatible models on execution pins by default.
- [ ] Preserve an existing selection as an explicit unavailable entry if a
      model disappears; never silently switch a patch to another model.
- [ ] Retain a documented object fallback and diagnostic when compatibility is
      unknown or the connected server lacks the model-catalog capability.
- [ ] Avoid repeatedly invalidating all node/workflow factories for catalog
      entry-only changes.
- [x] Add focused tests for provider models, local models, HF subtypes,
      structured serialization, label collisions, missing selections, and live
      catalog refresh.

### Gate

- [ ] Representative model pins appear as useful dynamic enums instead of
      `System.Object`.
- [ ] Installing a model updates the relevant enum without restarting vvvv and
      without changing its CLR pin type.
- [ ] Existing non-model pins and existing patch links remain unchanged.

## Phase 4 - Build the optional `VL.Nodetool.HDE` model manager

### Reviewed UI boundary

- [x] Confirm that editor-extension discovery, commands, docking, and automatic
      window restore require a `.HDE.vl` document and `WindowFactory`.
- [x] Confirm that compiled C# can technically provide the complete window
      content as a custom `VL.Skia.ILayer` or a separate desktop form.
- [x] Reject a hand-written C# Skia widget/input layer for the first version:
      it would duplicate layout, interaction, focus, and disposal behavior.
- [x] Reject WinForms, WPF, Avalonia, or a separate undocked form for the first
      version because it weakens the standard HDE docking/lifecycle path and
      adds dependencies.
- [x] Select a hybrid implementation: C# view-model/controller plus a small
      `VL.ImGui.Skia` UI patch hosted by the standard HDE `SkiaWindow`.
- [x] Build and package a minimal spike with target text, one model row, one
      action button, and one progress bar.
- [x] Interactively verify that the preview window opens, docks, resizes, and
      renders its ImGui controls.
- [ ] Interactively verify disposal and Shift+F9 restart with the live
      controller and an active or restored download.

### C# presentation layer

- [x] Add an editor-only model-manager controller/view-model over
      `IModelCatalog` and `IModelDownloadService`.
- [x] Expose cached scalar UI state and explicit commands; do not bind the UI
      directly to mutable transport DTOs or background callbacks.
- [x] Publish background results into locked cached state that the HDE reads on
      its frame, without blocking the editor's cooperative main loop.
- [x] Keep network requests, filtering, and progress aggregation out of the
      frame-driven VL patch.
- [x] Keep HDE-only dependencies out of `Nodetool.SDK`; isolate the imported
      editor process in the small `Nodetool.SDK.VL.HDE` adapter assembly.
- [x] Dispose subscriptions and cancel only HDE-owned requests on extension
      restart or window close; never cancel unrelated workflow execution.

### HDE window

- [x] Add `VL.Nodetool.HDE.vl` with the required `VL.HDE` and vvvv-shipped
      ImGui/Skia references and a command that opens a dockable model-manager
      window.
- [x] Reuse the portable C# model services and the configured NodeTool target;
      allow an editor-runtime catalog/download session when direct session
      sharing with the user runtime is unavailable.
- [x] Keep all HTTP/WebSocket and retry behavior behind the portable services;
      the HDE patch must not implement another protocol client.
- [x] Show which server and execution target the window is managing.
- [x] Group the first model inventory into a small set of model-type tabs
      (language, image, audio, video/3D, and other) while retaining exact
      compatibility filtering behind each tab.
- [x] Present ready local, ready remote, recommended/downloadable,
      downloading, failed, and unclassified models clearly.
- [x] Allow starting, cancelling, and retrying recommended model downloads.
- [x] Show concise per-model progress, downloaded/total size when known, and
      actionable errors.
- [x] Restore current server-side download states when the window opens or the
      extension restarts.
- [x] Refresh the catalog and corresponding VL enums after download completion.
- [x] Keep the first version focused on model inventory and downloads; do not
      add another node browser or workflow execution surface.
- [x] Ensure an HDE runtime error is contained in the optional HDE document and
      adapter assembly and does not compromise normal
      NodeTool nodes or other editor commands.

### Gate

- [ ] A user can open the HDE, see the connected server's model inventory,
      download a recommended model with visible progress, and then select it on
      a compatible node without restarting vvvv.
- [ ] Closing or omitting the HDE has no effect on model pins or execution.
- [ ] Opening or restarting the HDE during a download reconstructs accurate
      progress without starting a duplicate download.

## Phase 5 - Validation, documentation, and release

- [ ] Add backend contract tests for catalog parity with existing model
      services and download lifecycle events.
- [ ] Add C# integration tests against a live/local test server for catalog,
      selection, download progress, cancellation, and refresh.
- [x] Add VL tests that verify dynamic enum pin types and exact wire values.
- [ ] Run an interactive vvvv pass covering language, image, HF, llama, and
      missing/unavailable model selections.
- [ ] Test a connected remote worker/cloud target so downloads occur on the
      execution target rather than the vvvv computer.
- [ ] Verify that existing NodeTool web and Electron model selection and
      downloads remain unchanged.
- [x] Document availability terminology, model-pin behavior, refresh behavior,
      server capability requirements, and HDE installation/use.
- [x] Build and verify the `VL.Nodetool` package, including its portable SDK
      assemblies, HDE document, and isolated vvvv compile check.
- [ ] Build and verify the matching standalone `Nodetool.SDK` NuGet package.
- [ ] Publish only after the existing SDK execution/media regression suite and
      the new model tests pass.

## Deferred follow-ups

- [x] **Deferred:** automatically download a model merely because it was
      selected on an execution pin; explicit user action is safer initially.
- [x] **Deferred:** credentials editing and provider account management inside
      the HDE.
- [x] **Deferred:** uninstalling models, storage quotas, cache cleanup, and
      moving model storage.
- [x] **Deferred:** advanced recommendation ranking based on hardware, expected
      quality, speed, cost, or project history.
- [x] **Deferred:** an HDE node browser, graph editor, or general workflow test
      surface.
- [x] **Deferred:** automatic substitution when a selected model is missing;
      surface the problem and let the user choose.
- [x] **Deferred:** a compact search/filter row and full inventory list by
      compatibility, provider/source, and availability; the first HDE version
      intentionally shows one recommended candidate per broad family.
- [x] **Deferred:** reduce the full individual-node discovery payload/startup
      time with a server cache, compression, or a bounded incremental contract;
      keep complete pin metadata and saved-node identity intact.

## Likely implementation locations

### NodeTool

- `packages/protocol/src/api-schemas/`
- `packages/websocket/src/sdk/`
- `packages/websocket/src/trpc/routers/models.ts` as the existing service
  reference, not a second source of truth
- `packages/websocket/src/plugins/websocket.ts` and the existing download
  manager/worker relay

### Portable C# SDK

- `csharp/Nodetool.SDK/Api/Models/`
- `csharp/Nodetool.SDK/Models/`
- `csharp/Nodetool.SDK/Connection/`

### VL and HDE

- `csharp/Nodetool.SDK.VL/Utilities/VlTypeMapping.cs`
- `csharp/Nodetool.SDK.VL/Utilities/DynamicWorkflowEnumFactory.cs` as a pattern,
  while keeping mutable model enums separate
- `csharp/Nodetool.SDK.VL/Factories/`
- an isolated C# HDE presentation/controller area or assembly, if the spike
  confirms that this keeps editor-only dependencies out of normal consumers
- `vvvv/VL.Nodetool.HDE.vl`
- vvvv-shipped `VL.ImGui`, `VL.ImGui.Skia`, `VL.Skia`, and `VL.HDE`
- `vvvv-tests/`
