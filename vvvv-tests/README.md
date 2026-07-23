# vvvv SDK integration tests

This directory contains the manual and automated integration checks for the
NodeTool SDK package in vvvv gamma.

## Supported test target

The first release target is intentionally narrow:

- NodeTool protocol/package version: `0.7.0-rc.32`
- NodeTool SDK workflow contract: `workflow-interface` version `1`
- C# SDK assemblies: `0.1.1`
- VL package: `VL.Nodetool` `0.1.5`
- vvvv gamma: `7.0`
- `VL.Core`: `2025.7.0`
- .NET: `8.0`

The C# project and the VL NuGet package specification both reference
`VL.Core 2025.7.0`. vvvv gamma 7.1 / `VL.Core 2025.7.1` is a secondary
compatibility target, not part of the first acceptance gate.

Past NodeTool server or SDK contracts are not supported by this test target.

## Prepare NodeTool

From Git Bash:

```bash
cd /m/P/NODETOOL/____REPOS____/nodetool
NODETOOL_ENABLE_SDK_WORKFLOW_INTERFACE_V1=1 npm run dev
```

To run only the backend:

```bash
cd /m/P/NODETOOL/____REPOS____/nodetool
NODETOOL_ENABLE_SDK_WORKFLOW_INTERFACE_V1=1 npm run dev:server
```

Confirm that `http://127.0.0.1:7777/health` responds before opening a test
patch. Only one process may listen on port 7777.

## Build the SDK

From Git Bash:

```bash
cd /m/P/NODETOOL/____REPOS____/nodetool-sdk/csharp
dotnet build Nodetool.SDK.VL/Nodetool.SDK.VL.csproj -c Release
dotnet test Nodetool.SDK.Tests/Nodetool.SDK.Tests.csproj -c Release
```

The primary vvvv executable used for the release check is:

```text
C:\Program Files\vvvv\vvvv_gamma_7.0-0436-g633541ba2a-win-x64\vvvv.exe
```

## Workflow fixture set

Keep these small, clearly named workflows in the local NodeTool database.
Do not place large inline media in every fixture; only the large-graph fixture
should exercise that behavior.

- `SDK Test - Primitives`: string, integer, float, boolean, enum, and list pins.
- `SDK Test - Streaming Text`: append and replace updates followed by a terminal result.
- `SDK Test - Image Roundtrip`: image input and image output.
- `SDK Test - Cancellation`: a run long enough to cancel deterministically.
- `SDK Test - Rename Refresh`: safe to rename and edit while vvvv remains open.
- `SDK Test - Large Graph`: a large graph with representative inline image data.

Record the workflow IDs after creation. Stable IDs let a rename test distinguish
identity changes from display-name changes.

## Acceptance sequence

1. Open the discovery smoke patch with NodeTool online and confirm the workflow
   nodes appear without downloading full workflow graphs.
2. Run `SDK Test - Primitives` and compare every output value and type.
3. Run `SDK Test - Streaming Text` and confirm append/replace behavior and the
   terminal output.
4. Run `SDK Test - Image Roundtrip` with a small PNG and inspect the returned
   image.
5. Start and cancel `SDK Test - Cancellation`; confirm that vvvv does not remain
   in a running state.
6. Rename `SDK Test - Rename Refresh`, trigger refresh, and confirm that the node
   updates without restarting vvvv.
7. Stop and restart NodeTool while the patch remains open. The package must stay
   loadable offline and recover discovery after the server returns.
8. Open `SDK Test - Large Graph` and confirm discovery remains compact and does
   not transfer inline image data as a pin default.

When a check fails, save the vvvv patch, the NodeTool server log, and:

```text
%USERPROFILE%\Documents\vvvv\gamma\vvvv.log
```

The older `.vl` files in this directory reference historical build outputs.
They are useful as migration examples, but they are not the release acceptance
patches. Clean smoke patches and headless checks should use the current Release
assemblies or the locally packed `VL.Nodetool` package.
