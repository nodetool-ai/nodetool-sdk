using Microsoft.Extensions.Logging;
using Nodetool.SDK.Types;
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

        logger.LogInformation("Press any key to exit...");
        Console.ReadKey();
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
            var originalMessage = new WorkflowExecuteRequest
            {
                workflow_id = "test-workflow-123",
                inputs = new Dictionary<string, object>
                {
                    { "prompt", "Generate an AI image" },
                    { "width", 512 },
                    { "height", 512 }
                },
                job_id = Guid.NewGuid().ToString()
            };

            // Serialize
            var serializedData = typeLookup.Serialize(originalMessage);
            logger.LogInformation("   ✅ Serialized WorkflowExecuteRequest: {Size} bytes", serializedData.Length);

            // Deserialize
            var deserializedMessage = typeLookup.Deserialize<WorkflowExecuteRequest>(serializedData);
            
            if (deserializedMessage != null)
            {
                logger.LogInformation("   ✅ Deserialized WorkflowExecuteRequest: workflow_id={WorkflowId}", 
                    deserializedMessage.workflow_id);
                
                if (deserializedMessage.workflow_id == originalMessage.workflow_id)
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
} 