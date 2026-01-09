using System;
using System.Linq;
using System.IO;
using System.Reflection;
using VL.Core;
using VL.Core.CompilerServices;
using Nodetool.SDK.VL.Factories;
using Nodetool.SDK.VL.Utilities;

namespace Nodetool.SDK.VL
{
    /// <summary>
    /// VL assembly initializer for Nodetool SDK
    /// </summary>
    public sealed class Initialization : AssemblyInitializer<Initialization>
    {
        public Initialization()
        {
            try
            {
                var asm = typeof(Initialization).Assembly;
                var loc = asm.Location;
                var ver = asm.GetName().Version?.ToString() ?? "unknown";
                var lastWrite = (!string.IsNullOrWhiteSpace(loc) && File.Exists(loc))
                    ? File.GetLastWriteTimeUtc(loc).ToString("O")
                    : "unknown";

                // Keep startup log concise; full dumps are behind NODETOOL_VL_VERBOSE=1.
                VlLog.Info($"loaded v{ver} from '{loc}' (lastWriteUtc={lastWrite})");
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        /// Called by VL to configure this assembly for an AppHost.
        /// Registers the workflow node factory.
        /// </summary>
        public override void Configure(AppHost appHost)
        {
            VlLog.Debug($"Configure() appHost={appHost?.GetType().Name ?? "null"}");
            
            try
            {
                DumpLoadedAssemblies();

                // Register diagnostics first so Connect is always available (even if API calls fail)
                appHost?.RegisterNodeFactory("Nodetool",
                    vlSelfFactory => DiagnosticsNodeFactory.GetFactory(vlSelfFactory)
                );
                
                // Register the workflow node factory
                appHost?.RegisterNodeFactory("Nodetool.Workflows", 
                    vlSelfFactory => WorkflowNodeFactory.GetFactory(vlSelfFactory)
                );
                
                // Register the individual nodes factory
                appHost?.RegisterNodeFactory("Nodetool.Nodes", 
                    vlSelfFactory => NodesFactory.GetFactory(vlSelfFactory)
                );

                // Asset utility nodes (download/cache/upload helpers)
                appHost?.RegisterNodeFactory("Nodetool.Assets",
                    vlSelfFactory => AssetNodeFactory.GetFactory(vlSelfFactory)
                );

                VlLog.Info("registered factories (Diagnostics, Workflows, Nodes, Assets)");
            }
            catch (Exception ex)
            {
                VlLog.Error($"factory registration failed: {ex.GetType().Name}: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    VlLog.Error($"inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                }
                
                throw;
            }
        }

        private static void DumpLoadedAssemblies()
        {
            try
            {
                if (!VlLog.Verbose)
                    return;

                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a =>
                    {
                        var name = a.GetName().Name ?? "";
                        return name.StartsWith("Nodetool.", StringComparison.OrdinalIgnoreCase)
                               || name.StartsWith("MessagePack", StringComparison.OrdinalIgnoreCase)
                               || name.StartsWith("VL.", StringComparison.OrdinalIgnoreCase);
                    })
                    .OrderBy(a => a.GetName().Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                VlLog.Debug("Loaded assemblies (filtered):");
                foreach (var a in assemblies)
                {
                    var name = a.GetName();
                    var location = "(dynamic)";
                    try { location = a.Location; } catch { /* ignored */ }

                    Console.WriteLine($"- {name.Name}, Version={name.Version}, Location={location}");
                }

                // Extra: explicitly show what 'Nodetool.SDK' the runtime resolved (if any)
                var sdkAsm = assemblies.FirstOrDefault(a =>
                    string.Equals(a.GetName().Name, "Nodetool.SDK", StringComparison.OrdinalIgnoreCase));
                if (sdkAsm != null)
                {
                    var hasIExecutionSession = sdkAsm.GetType("Nodetool.SDK.Execution.IExecutionSession", throwOnError: false) != null;
                    VlLog.Debug($"Nodetool.SDK has IExecutionSession = {hasIExecutionSession}");
                }
            }
            catch (Exception ex)
            {
                VlLog.Error($"Failed to dump loaded assemblies: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
} 