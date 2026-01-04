using Microsoft.Extensions.Logging;
using Nodetool.SDK.Api;
using Nodetool.SDK.Configuration;
using Nodetool.SDK.Execution;
using Nodetool.SDK.Types;
using Nodetool.SDK.Values;
using Nodetool.SDK.WebSocket;

namespace Nodetool.SDK.TestConsole;

/// <summary>
/// Simple test console to validate the NodeTool type registry system.
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
            await RunWorkflowMode(args, loggerFactory, logger);
            return;
        }

        logger.LogInformation("🚀 NodeTool SDK Type Registry Test Console");
        logger.LogInformation("========================================");

        try
        {
            // Initialize type registries
            var typeRegistry = new NodeToolTypeRegistry(loggerFactory.CreateLogger<NodeToolTypeRegistry>());
            var enumRegistry = new EnumRegistry(loggerFactory.CreateLogger<EnumRegistry>());
            var typeLookup = new TypeLookupService(typeRegistry, enumRegistry, loggerFactory.CreateLogger<TypeLookupService>());

            logger.LogInformation("🔍 Initializing type system...");
            typeLookup.Initialize();

            // Get type system info
            var typeInfo = typeLookup.GetTypeSystemInfo();
            logger.LogInformation("📊 Type System Summary:");
            logger.LogInformation("   - Total Types: {TotalTypes}", typeInfo.TotalTypes);
            logger.LogInformation("   - Total Enums: {TotalEnums}", typeInfo.TotalEnums);

            // Show types by category
            logger.LogInformation("📁 Types by Category:");
            foreach (var category in typeInfo.TypesByCategory)
            {
                logger.LogInformation("   - {Category}: {Count} types", category.Key, category.Value.Count);
                
                // Show first few types as examples
                var examples = category.Value.Take(3).ToList();
                if (examples.Any())
                {
                    logger.LogInformation("     Examples: {Examples}", string.Join(", ", examples));
                }
            }

            // Show enums by category
            logger.LogInformation("🔢 Enums by Category:");
            foreach (var category in typeInfo.EnumsByCategory)
            {
                logger.LogInformation("   - {Category}: {Count} enums", category.Key, category.Value.Count);
            }

            // Test specific type lookups
            logger.LogInformation("🧪 Testing specific type lookups...");
            await TestTypeLookups(typeLookup, logger);

            // Test WebSocket message types
            logger.LogInformation("📡 Testing WebSocket message types...");
            await TestWebSocketMessages(typeLookup, logger);

            // Test MessagePack serialization
            logger.LogInformation("📦 Testing MessagePack serialization...");
            await TestMessagePackSerialization(typeLookup, logger);

            logger.LogInformation("✅ All tests completed successfully!");

        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Test failed with error");
        }
    }

    static async Task TestTypeLookups(TypeLookupService typeLookup, ILogger logger)
    {
        // Test common asset types
        var testTypes = new[] { "image", "audio", "video", "hf.stable_diffusion", "comfy.conditioning" };

        foreach (var typeName in testTypes)
        {
            var type = typeLookup.GetTypesByCategory().Values
                .SelectMany(types => types)
                .FirstOrDefault(t => t == typeName);

            if (type != null)
            {
                logger.LogInformation("   ✅ Found type: {TypeName}", typeName);
            }
            else
            {
                logger.LogWarning("   ⚠️  Type not found: {TypeName}", typeName);
            }
        }
    }

    static async Task TestWebSocketMessages(TypeLookupService typeLookup, ILogger logger)
    {
        try
        {
            // Test JobUpdate
            var jobUpdate = new JobUpdate
            {
                status = "completed",
                job_id = "test-job-123",
                message = "Test job completed successfully"
            };

            var typeName = typeLookup.GetTypeName(jobUpdate);
            logger.LogInformation("   ✅ JobUpdate type name: {TypeName}", typeName);

            // Test NodeUpdate
            var nodeUpdate = new NodeUpdate
            {
                node_id = "node-456",
                node_name = "TestNode",
                status = "completed",
                result = new Dictionary<string, object> { { "output", "test result" } }
            };

            typeName = typeLookup.GetTypeName(nodeUpdate);
            logger.LogInformation("   ✅ NodeUpdate type name: {TypeName}", typeName);

            // Test OutputUpdate
            var outputUpdate = new OutputUpdate
            {
                node_id = "node-789",
                node_name = "OutputNode",
                output_name = "result",
                value = "test output value",
                output_type = "string"
            };

            typeName = typeLookup.GetTypeName(outputUpdate);
            logger.LogInformation("   ✅ OutputUpdate type name: {TypeName}", typeName);

            // Test PreviewUpdate
            var previewUpdate = new PreviewUpdate
            {
                node_id = "node-101",
                value = new Dictionary<string, object> { { "type", "image" }, { "uri", "data:..." } }
            };

            typeName = typeLookup.GetTypeName(previewUpdate);
            logger.LogInformation("   ✅ PreviewUpdate type name: {TypeName}", typeName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "   ❌ WebSocket message test failed");
        }
    }

    static async Task TestMessagePackSerialization(TypeLookupService typeLookup, ILogger logger)
    {
        try
        {
            // Test serialization/deserialization round-trip
            var originalMessage = new WebSocketCommand
            {
                command = "run_job",
                type = "run_job",
                data = new RunJobRequest
                {
                    WorkflowId = "test-workflow-123",
                    JobType = "workflow",
                    Params = new Dictionary<string, object>
                    {
                        { "prompt", "Generate an AI image" },
                        { "width", 512 },
                        { "height", 512 }
                    },
                    ExplicitTypes = true
                }
            };

            // Serialize
            var serializedData = typeLookup.Serialize(originalMessage);
            logger.LogInformation("   ✅ Serialized run_job WebSocketCommand: {Size} bytes", serializedData.Length);

            // Deserialize
            var deserializedMessage = typeLookup.Deserialize<WebSocketCommand>(serializedData);
            
            if (deserializedMessage != null)
            {
                logger.LogInformation("   ✅ Deserialized WebSocketCommand: command={Command}, type={Type}", 
                    deserializedMessage.command, deserializedMessage.type);
                
                if (deserializedMessage.command == originalMessage.command)
                {
                    logger.LogInformation("   ✅ Round-trip serialization successful!");
                }
                else
                {
                    logger.LogWarning("   ⚠️  Round-trip data mismatch");
                }
            }
            else
            {
                logger.LogWarning("   ⚠️  Deserialization returned null");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "   ❌ MessagePack serialization test failed");
        }
    }

    private static async Task RunWorkflowMode(string[] args, ILoggerFactory loggerFactory, ILogger logger)
    {
        var ws = GetArgValue(args, "--ws") ?? Environment.GetEnvironmentVariable("NODETOOL_WORKER_WS");
        var http = GetArgValue(args, "--http") ?? Environment.GetEnvironmentVariable("NODETOOL_HTTP_API");
        var workflowName = GetArgValue(args, "--workflow") ?? "TEST_SDK_01";
        var timeoutSecStr = GetArgValue(args, "--timeout-sec") ?? "30";
        var timeoutSec = int.TryParse(timeoutSecStr, out var parsedTimeout) ? parsedTimeout : 30;

        if (string.IsNullOrWhiteSpace(ws) || string.IsNullOrWhiteSpace(http))
        {
            logger.LogError("Missing required URLs. Provide --ws and --http (or env vars NODETOOL_WORKER_WS and NODETOOL_HTTP_API).");
            logger.LogInformation("Example:");
            logger.LogInformation("  dotnet run -c Release -- run-workflow --ws ws://localhost:7777/ws --http http://localhost:7777 --workflow TEST_SDK_01");
            return;
        }

        logger.LogInformation("🚀 NodeTool SDK Workflow Runner (WebSocket)");
        logger.LogInformation("  WS:   {Ws}", ws);
        logger.LogInformation("  HTTP: {Http}", http);
        logger.LogInformation("  Workflow: {Workflow}", workflowName);

        var options = new NodeToolClientOptions
        {
            WorkerWebSocketUrl = new Uri(ws),
            ApiBaseUrl = new Uri(http),
        };

        // Discover workflow id + input schema via HTTP
        using var api = new NodetoolClient(logger: loggerFactory.CreateLogger<NodetoolClient>());
        api.Configure(options.ApiBaseUrl!.ToString().TrimEnd('/'));

        var workflows = await api.GetWorkflowsAsync();
        var wf = workflows.FirstOrDefault(w => string.Equals(w.Name, workflowName, StringComparison.OrdinalIgnoreCase));
        if (wf == null)
        {
            logger.LogError("Workflow not found: {Workflow}. Available workflows: {Names}", workflowName, string.Join(", ", workflows.Select(w => w.Name)));
            return;
        }

        // Build inputs. Prefer command-line key=value pairs; otherwise seed required inputs with a demo value.
        var inputs = ParseInputs(args);
        if (inputs.Count == 0)
        {
            var schema = wf.InputSchema;
            if (schema?.Properties != null && schema.Properties.Count > 0)
            {
                var required = schema.Required ?? new List<string>();
                if (required.Count == 0)
                {
                    // If required list isn't present, at least set the first property for convenience.
                    var first = schema.Properties.Keys.OrderBy(k => k, StringComparer.Ordinal).First();
                    inputs[first] = "hello from c#";
                }
                else
                {
                    foreach (var key in required)
                    {
                        inputs[key] = "hello from c#";
                    }
                }
            }
        }

        logger.LogInformation("Resolved workflow id: {WorkflowId}", wf.Id);
        logger.LogInformation("Sending inputs: {Inputs}", string.Join(", ", inputs.Select(kvp => $"{kvp.Key}={kvp.Value}")));

        using var exec = new NodeToolExecutionClient(options, logger: loggerFactory.CreateLogger<NodeToolExecutionClient>());
        if (!await exec.ConnectAsync())
        {
            logger.LogError("Failed to connect to worker WS: {Ws}", ws);
            return;
        }

        var session = await exec.ExecuteWorkflowAsync(wf.Id, inputs);
        session.OutputReceived += update =>
        {
            logger.LogInformation("output_update: node={NodeName} output={OutputName} type={OutputType} value={Value}",
                update.NodeName, update.OutputName, update.OutputType, update.Value.AsString() ?? update.Value.ToJsonString());
        };
        session.PreviewReceived += update =>
        {
            logger.LogInformation("preview_update: node={NodeId} value={Value}", update.NodeId, update.Value.TypeDiscriminator ?? update.Value.AsString() ?? update.Value.ToJsonString());
        };
        session.NodeUpdated += update =>
        {
            if (!string.IsNullOrWhiteSpace(update.error))
            {
                logger.LogWarning("node_update error: node={NodeName} error={Error}", update.node_name, update.error);
            }
        };
        session.Completed += (ok, msg) =>
        {
            logger.LogInformation("completed: ok={Ok} message={Message}", ok, msg ?? "");
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSec));
        var ok = await session.WaitForCompletionAsync(cts.Token);
        if (!ok)
        {
            logger.LogWarning("Session did not complete within {Timeout}s (or was cancelled).", timeoutSec);
        }

        var outputs = session.GetLatestOutputs();
        if (outputs.Count > 0)
        {
            logger.LogInformation("Final outputs ({Count}):", outputs.Count);
            foreach (var kvp in outputs.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                logger.LogInformation("  {Key} = {Value}", kvp.Key, kvp.Value.AsString() ?? kvp.Value.ToJsonString());
            }
        }
        else
        {
            logger.LogInformation("No outputs captured.");
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

            result[key] = val;
        }

        return result;
    }
} 