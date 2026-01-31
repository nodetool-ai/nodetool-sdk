# Nodetool SDK Unity Package

This package wraps the core `Nodetool.SDK` DLLs for Unity usage. It ships Unity-facing helpers and sample content while keeping the core SDK free of Unity dependencies.

## Setup

1. Build `Nodetool.SDK` for `netstandard2.1` and copy the DLLs (plus dependencies) into `Runtime/Plugins`.
2. Create a `NodetoolSettings` asset under `Resources/NodetoolSettings` and configure your endpoints.
3. Open the BasicWorkflow sample scene to run a workflow end-to-end.

## Runtime Components

- `NodetoolBridge` (MonoBehaviour singleton)
- `MainThreadDispatcher` (marshals callbacks to the main thread)
- `UnityAssetManager` (Texture2D/audio helpers)

## Samples

See `Samples~/BasicWorkflow` for a minimal scene and script.
