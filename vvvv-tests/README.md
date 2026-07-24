# vvvv SDK integration tests

This directory contains the manual and automated integration checks for the
NodeTool SDK package in vvvv gamma.

## Supported test target

The first release target is intentionally narrow:

- NodeTool protocol/package version: `0.7.0-rc.32`
- NodeTool SDK workflow contract: `workflow-interface` version `1`
- C# SDK assemblies: `0.1.1`
- VL package: `VL.Nodetool` `0.1.5`
- vvvv gamma: `7.1`
- `VL.Core`: `2025.7.1`
- .NET: `8.0`

The C# project, the VL NuGet package specification, and the main VL document
all target the 2025.7.1 toolchain. vvvv gamma 7.0 is not part of the first
acceptance gate.

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
dotnet test Nodetool.SDK.VL.Tests/Nodetool.SDK.VL.Tests.csproj -c Release
```

The headless VL tests use `VVVV_EXE` or `VVVV_HOME` when set, then look for an
installed `vvvv_gamma_7.1-*` under `C:\Program Files\vvvv`. The same checks can
be run through:

```bash
powershell.exe -NoProfile -ExecutionPolicy Bypass \
  -File ./regen-and-verify.ps1 \
  -SkipGeneration -SkipGitDiff -IncludeVL -IncludeVLTests -VerifyVLPackage
```

The primary vvvv executable used for the release check is:

```text
C:\Program Files\vvvv\vvvv_gamma_7.1-0070-g54e23a3bf8-win-x64\vvvv.exe
```

## Workflow fixture set

Keep these small, clearly named workflows in the local NodeTool database.
Do not place large inline media in every fixture; only the large-graph fixture
should exercise that behavior.

- `SDK Test - Primitives`: string, integer, float, boolean, enum, and list pins.
- `SDK Test - Streaming Text`: append and replace updates followed by a terminal result.
- `SDK Test - Image Roundtrip`: image input and image output.
- `SDK Test - Media References`: audio, video, document, and generic asset
  inputs directly connected to matching workflow outputs.
- `SDK Test - Cancellation`: a run long enough to cancel deterministically.
- `SDK Test - Rename Refresh`: safe to rename and edit while vvvv remains open.
- `SDK Test - Large Graph`: a large graph with representative inline image data.

Created in the local development database on 2026-07-23:

- `SDK Test - Primitives` (`3be02581903641aab774eef40ac164b0`): string,
  integer, float, boolean, select/enum, and string-list roundtrips.
- `SDK Test - Image Roundtrip` (`67ec9fedc4a84d639c42d72cc2a36500`):
  image input directly connected to a generic workflow output.
- `SDK Test - Cancellation` (`5997d9c52e1f48818b20507d34908d32`):
  string input through a 30-second `Wait`, then to an output.

Their authoritative v1 interfaces return the expected public pins with zero
diagnostics. The superseded two-pin primitive draft
(`dbeab8bbc7a84b569b5b94a4f9342fbd`) was deleted after the backend restarted
with workflow PUT/DELETE route registration commit `323812eb0`.

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
5. Run `SDK Test - Media References` with small local or uploaded fixtures.
   Confirm that file-backed inputs are native `Path` pins, outputs remain typed
   asset references, and `Nodetool.Assets -> AssetAsFile` produces native,
   usable paths for the audio, video, and document results. Run it twice and
   confirm the second materialization reports `FromCache`.
6. Start and cancel `SDK Test - Cancellation`; confirm that vvvv does not remain
   in a running state.
7. Rename `SDK Test - Rename Refresh`, trigger refresh, and confirm that the node
   updates without restarting vvvv.
8. Stop and restart NodeTool while the patch remains open. The package must stay
   loadable offline and recover discovery after the server returns.
9. Open `SDK Test - Large Graph` and confirm discovery remains compact and does
   not transfer inline image data as a pin default.

When a check fails, save the vvvv patch, the NodeTool server log, and:

```text
%USERPROFILE%\Documents\vvvv\gamma\vvvv.log
```

The older `.vl` files in this directory reference historical build outputs.
They are useful as migration examples, but they are not the release acceptance
patches. Clean smoke patches and headless checks should use the current Release
assemblies or the locally packed `VL.Nodetool` package.

The gamma 7.1 headless audit confirms that none of the seven historical files
is a current smoke fixture:

- `VL.Nodetool.vl`, `VL.NodetoolNodes.vl`, and
  `VL.NodetoolWorkflows.vl` reference retired node-factory dependency names.
- `Test_01.vl`, `Test_02.vl`, `NODETOOL_VL_TEST_01.vl`, and
  `NODETOOL_VL_SPEED_TEST_01.vl` embed local workflow nodes and pin names from
  obsolete contracts.

Changing their DLL paths would not make them valid. Keep them as migration
evidence and create the current smoke set from the named workflow fixtures
above.
