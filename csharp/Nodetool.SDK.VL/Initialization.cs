using System;
using VL.Core;
using VL.Core.CompilerServices;
using Nodetool.SDK.VL.Factories;

namespace Nodetool.SDK.VL
{
    /// <summary>
    /// VL assembly initializer for Nodetool SDK
    /// </summary>
    public sealed class Initialization : AssemblyInitializer<Initialization>
    {
        public Initialization()
        {
            Console.WriteLine("=== Nodetool.SDK.VL: Assembly initializer created ===");
            Console.WriteLine($"Nodetool.SDK.VL: Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        }

        /// <summary>
        /// Called by VL to configure this assembly for an AppHost.
        /// Registers the workflow node factory.
        /// </summary>
        public override void Configure(AppHost appHost)
        {
            Console.WriteLine("=== Nodetool.SDK.VL: Configure called - registering workflow factory ===");
            Console.WriteLine($"Nodetool.SDK.VL: AppHost type: {appHost?.GetType().Name ?? "null"}");
            
            try
            {
                Console.WriteLine("Nodetool.SDK.VL: About to register factories...");
                
                // Register the workflow node factory
                Console.WriteLine("Nodetool.SDK.VL: Registering WorkflowNodeFactory...");
                appHost?.RegisterNodeFactory("Nodetool.SDK.VL.Workflows", 
                    vlSelfFactory => WorkflowNodeFactory.GetFactory(vlSelfFactory)
                );
                Console.WriteLine("Nodetool.SDK.VL: WorkflowNodeFactory registered");
                
                // Register the individual nodes factory
                Console.WriteLine("Nodetool.SDK.VL: Registering NodesFactory...");
                appHost?.RegisterNodeFactory("Nodetool.SDK.VL.Nodes", 
                    vlSelfFactory => NodesFactory.GetFactory(vlSelfFactory)
                );
                Console.WriteLine("Nodetool.SDK.VL: NodesFactory registered");
                
                Console.WriteLine("=== Nodetool.SDK.VL: All factories registered successfully ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== Nodetool.SDK.VL: ERROR registering workflow factory ===");
                Console.WriteLine($"Nodetool.SDK.VL: Error message: {ex.Message}");
                Console.WriteLine($"Nodetool.SDK.VL: Error type: {ex.GetType().Name}");
                Console.WriteLine($"Nodetool.SDK.VL: Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Nodetool.SDK.VL: Inner exception: {ex.InnerException.Message}");
                }
                
                throw;
            }
        }
    }
} 