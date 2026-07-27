using Microsoft.Extensions.Logging;
using System.Text.Json;
using Nodetool.SDK.Api;
using Nodetool.SDK.Configuration;
using Nodetool.SDK.Diagnostics;
using Nodetool.SDK.Execution;
using Nodetool.SDK.Values;
using Nodetool.SDK.Workflows;

namespace Nodetool.SDK.TestConsole;

/// <summary>
/// Simple workflow-only console consumer of the portable NodeTool SDK.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        var logger = loggerFactory.CreateLogger<Program>();

        if (args.Contains("run-workflow", StringComparer.OrdinalIgnoreCase))
        {
            await RunModeSafelyAsync(() => RunWorkflowMode(args, loggerFactory, logger), logger);
            return;
        }

        if (args.Contains("fetch", StringComparer.OrdinalIgnoreCase))
        {
            await RunModeSafelyAsync(() => FetchMode(args, loggerFactory, logger), logger);
            return;
        }

        logger.LogInformation("NodeTool SDK smoke-test console");
        logger.LogInformation(
            "Use 'fetch --ws <url>' or 'run-workflow --ws <url> --workflow <name-or-id>'.");
    }

    private static async Task RunModeSafelyAsync(Func<Task> run, ILogger logger)
    {
        try
        {
            await run();
        }
        catch (Exception ex)
        {
            logger.LogError(
                "NodeTool SDK smoke test failed: {Error}",
                NodeToolDiagnosticRedactor.RedactText(ex.Message));
            Environment.ExitCode = 1;
        }
    }

    private static async Task FetchMode(string[] args, ILoggerFactory loggerFactory, ILogger logger)
    {
        var ws = GetArgValue(args, "--ws") ?? Environment.GetEnvironmentVariable("NODETOOL_WORKER_WS");
        if (string.IsNullOrWhiteSpace(ws))
        {
            logger.LogError("Missing --ws. Provide --ws <url> or set NODETOOL_WORKER_WS.");
            logger.LogInformation("Example: dotnet run -c Release -- fetch --ws ws://localhost:7777/ws");
            return;
        }

        logger.LogInformation("🔍 NodeTool SDK Fetch Test (WebSocket)");
        logger.LogInformation(
            "  WS: {Ws}",
            NodeToolDiagnosticRedactor.RedactText(ws));

        var options = new NodeToolClientOptions { WorkerWebSocketUrl = new Uri(ws) };
        using var exec = new NodeToolExecutionClient(options, logger: loggerFactory.CreateLogger<NodeToolExecutionClient>());

        if (!await exec.ConnectAsync())
        {
            logger.LogError("Failed to connect to {Ws}", ws);
            return;
        }

        logger.LogInformation("--- list_nodes ---");
        var nodes = await exec.GetNodeTypesAsync();
        logger.LogInformation("Got {Count} node types", nodes.Count);
        foreach (var n in nodes.Take(5))
            logger.LogInformation("  {NodeType}: {Title}", n.NodeType, n.Title);
        if (nodes.Count > 5) logger.LogInformation("  ... and {More} more", nodes.Count - 5);

        if (nodes.Count > 0)
        {
            logger.LogInformation("--- get_node ---");
            var singleNode = await exec.GetNodeAsync(nodes[0].NodeType);
            logger.LogInformation("  {NodeType}: {Title}", singleNode?.NodeType, singleNode?.Title);
        }

        logger.LogInformation("--- workflow catalog ---");
        using var catalog = new WorkflowCatalog(
            exec,
            ws,
            TimeSpan.Zero,
            logger: loggerFactory.CreateLogger<WorkflowCatalog>());
        var workflowSnapshot = await catalog.RefreshAsync();
        logger.LogInformation(
            "Got {Count} workflow descriptors ({CacheHits} cached, {Skipped} skipped)",
            workflowSnapshot.Workflows.Count,
            workflowSnapshot.CacheHitCount,
            workflowSnapshot.SkippedCount);
        foreach (var workflow in workflowSnapshot.Workflows)
            logger.LogInformation(
                "  [{Id}] {Name}: {Inputs} inputs, {Outputs} outputs",
                workflow.Id,
                workflow.Name,
                workflow.Inputs.Count,
                workflow.Outputs.Count);

        if (workflowSnapshot.Workflows.Count > 0)
        {
            logger.LogInformation("--- get_workflow ---");
            var single = await exec.GetWorkflowAsync(workflowSnapshot.Workflows[0].Id);
            logger.LogInformation("  {Name} — inputs: {InputCount}, graph nodes: {NodeCount}",
                single?.Name,
                single?.InputSchema?.Properties?.Count ?? 0,
                single?.Graph?.Nodes.Count ?? 0);
        }

        logger.LogInformation("--- list_assets ---");
        var assets = await exec.GetAssetsAsync(pageSize: 10);
        logger.LogInformation("Got {Count} assets (page_size 10)", assets.Count);
        foreach (var a in assets)
            logger.LogInformation("  [{Id}] {Name} ({ContentType})", a.Id, a.Name, a.ContentType);

        if (assets.Count > 0)
        {
            logger.LogInformation("--- get_asset ---");
            var singleAsset = await exec.GetAssetAsync(assets[0].Id);
            logger.LogInformation("  {Name} ({ContentType})", singleAsset?.Name, singleAsset?.ContentType);
        }

        await exec.DisconnectAsync();
        logger.LogInformation("✅ Fetch test complete.");
    }

    private static async Task RunWorkflowMode(string[] args, ILoggerFactory loggerFactory, ILogger logger)
    {
        var ws = GetArgValue(args, "--ws") ?? Environment.GetEnvironmentVariable("NODETOOL_WORKER_WS");
        var workflowName = GetArgValue(args, "--workflow") ?? "TEST_SDK_01";
        var timeoutSecStr = GetArgValue(args, "--timeout-sec") ?? "30";
        var timeoutSec = int.TryParse(timeoutSecStr, out var parsedTimeout) ? parsedTimeout : 30;

        if (string.IsNullOrWhiteSpace(ws))
        {
            logger.LogError("Missing required URL. Provide --ws (or env var NODETOOL_WORKER_WS).");
            logger.LogInformation("Example:");
            logger.LogInformation("  dotnet run -c Release -- run-workflow --ws ws://localhost:7777/ws --workflow TEST_SDK_01");
            return;
        }

        logger.LogInformation("🚀 NodeTool SDK Workflow Runner (WebSocket)");
        logger.LogInformation(
            "  WS:       {Ws}",
            NodeToolDiagnosticRedactor.RedactText(ws));
        logger.LogInformation("  Workflow: {Workflow}", workflowName);

        var options = new NodeToolClientOptions
        {
            WorkerWebSocketUrl = new Uri(ws),
        };

        // Build inputs. Prefer command-line key=value pairs; otherwise seed required inputs with a demo value.
        var inputs = ParseInputs(args);
        var seededDefault = inputs.Count == 0;
        if (seededDefault)
        {
            // Seed a helpful default for simple workflows.
            inputs["string_input_1"] = "hello from c#";
        }

        logger.LogInformation(
            "Sending inputs: {Inputs}",
            NodeToolDiagnosticRedactor.RedactWorkflowInputs(
                inputs.ToDictionary(
                    pair => pair.Key,
                    pair => (object?)pair.Value,
                    StringComparer.Ordinal)));

        using var exec = new NodeToolExecutionClient(options, logger: loggerFactory.CreateLogger<NodeToolExecutionClient>());
        if (!await exec.ConnectAsync())
        {
            logger.LogError("Failed to connect to worker WS: {Ws}", ws);
            return;
        }

        using var catalog = new WorkflowCatalog(
            exec,
            scope: ws,
            logger: loggerFactory.CreateLogger<WorkflowCatalog>());
        var catalogSnapshot = await catalog.RefreshAsync(force: true);
        var workflow = catalogSnapshot.Workflows.FirstOrDefault(candidate =>
            string.Equals(
                candidate.Name,
                workflowName,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                candidate.Id,
                workflowName,
                StringComparison.Ordinal));
        if (workflow == null)
        {
            logger.LogError(
                "Workflow '{Workflow}' was not found in {Count} descriptors.",
                workflowName,
                catalogSnapshot.Workflows.Count);
            await exec.DisconnectAsync();
            return;
        }

        await using var controller = new WorkflowExecutionController(
            exec,
            workflow.Outputs);
        var loggedOutputs = new Dictionary<string, DateTimeOffset>(
            StringComparer.Ordinal);
        var lastState = WorkflowExecutionState.Idle;
        controller.SnapshotChanged += snapshot =>
        {
            if (snapshot.State != lastState)
            {
                lastState = snapshot.State;
                logger.LogInformation(
                    "state={State} progress={Progress:P0}",
                    snapshot.State,
                    snapshot.Progress);
            }

            foreach (var output in snapshot.Outputs.Values)
            {
                if (loggedOutputs.TryGetValue(
                        output.PublicName,
                        out var loggedAt) &&
                    loggedAt >= output.UpdatedAt)
                {
                    continue;
                }
                loggedOutputs[output.PublicName] = output.UpdatedAt;
                var renderedValue =
                    output.Value.Kind == NodeToolValueKind.String
                        ? output.Value.AsString() ?? ""
                        : output.Value.ToJsonString();
                logger.LogInformation(
                    "output: name={Name} streaming={Streaming} done={Done} value={Value}",
                    output.PublicName,
                    output.IsStreaming,
                    output.Done,
                    renderedValue);
            }
        };

        await controller.StartAsync(new WorkflowInvocation(
            workflow.Id,
            inputs.ToDictionary(
                pair => pair.Key,
                pair => (object?)pair.Value,
                StringComparer.Ordinal),
            TimeSpan.FromSeconds(timeoutSec)));
        await controller.WaitForTerminalAsync();

        var terminal = controller.Snapshot;
        if (terminal.Outputs.Count > 0)
        {
            logger.LogInformation("Final outputs ({Count}):", terminal.Outputs.Count);
            foreach (var output in terminal.Outputs.Values.OrderBy(
                         output => output.PublicName,
                         StringComparer.Ordinal))
            {
                var rendered = output.Value.Kind == NodeToolValueKind.String
                    ? output.Value.AsString() ?? ""
                    : output.Value.ToJsonString();
                logger.LogInformation(
                    "  {Name} = {Value}",
                    output.PublicName,
                    rendered);
            }
        }
        else
        {
            logger.LogInformation("No outputs captured.");
        }
        if (terminal.State != WorkflowExecutionState.Completed)
        {
            logger.LogWarning(
                "Workflow ended in {State}: {Error}",
                terminal.State,
                terminal.Error ?? "");
        }

        await exec.DisconnectAsync();
    }

    private static string? GetArgValue(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return null;
    }

    private static Dictionary<string, object> ParseInputs(string[] args)
    {
        var result = new Dictionary<string, object>(StringComparer.Ordinal);

        // Supports repeated: --input key=value
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (!string.Equals(args[i], "--input", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var token = args[i + 1];
            var eq = token.IndexOf('=');
            if (eq <= 0) continue;

            var key = token[..eq].Trim();
            var val = token[(eq + 1)..];
            if (key.Length == 0) continue;

            result[key] = ParseInputValue(val);
        }

        return result;
    }

    private static object ParseInputValue(string value)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            return JsonElementToObject(document.RootElement) ?? "";
        }
        catch (JsonException)
        {
            if (value.Length >= 2 &&
                value[0] == '[' &&
                value[^1] == ']')
            {
                return value[1..^1]
                    .Split(',', StringSplitOptions.TrimEntries |
                                StringSplitOptions.RemoveEmptyEntries)
                    .Select(ParseInputValue)
                    .ToArray();
            }
            return value;
        }
    }

    private static object? JsonElementToObject(JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.Object => value.EnumerateObject().ToDictionary(
                property => property.Name,
                property => JsonElementToObject(property.Value),
                StringComparer.Ordinal),
            JsonValueKind.Array => value.EnumerateArray()
                .Select(JsonElementToObject)
                .ToArray(),
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number when value.TryGetInt32(out var integer)
                => integer,
            JsonValueKind.Number when value.TryGetInt64(out var longInteger)
                => longInteger,
            JsonValueKind.Number => value.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => value.ToString()
        };
}
