# Nodetool SDK

SDK and integration for NodeTool clients.

## C# and VL builds

From `csharp/`:

```powershell
.\regen-and-verify.ps1 -IncludeVL -SkipGeneration -SkipGitDiff -VerifySdkPackage
```

For an already-restored or offline workspace, add `-NoRestore`. Verification
stops immediately if a `dotnet` build, test or pack command fails.

Default build output:

- `csharp/_vvvv_builds/Release/net8.0/`

Override output folder:

```powershell
.\regen-and-verify.ps1 -IncludeVL -OutputDir "C:\path\to\output"
```

## Electron local connection default

When using the NodeTool Electron app, backend binds to localhost and selects port `7777` by default (next free port if occupied):

- WebSocket: `ws://127.0.0.1:<port>/ws`
- HTTP API: `http://127.0.0.1:<port>`
