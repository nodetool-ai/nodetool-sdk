using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using VL.Core;
using VL.Core.CompilerServices;
using VL.Core.Diagnostics;
using Nodetool.SDK.VL.Models;
using Nodetool.SDK.VL.Utilities;
using SkiaSharp;

namespace Nodetool.SDK.VL.Nodes
{
    /// <summary>
    /// Node description for Nodetool workflow nodes in VL
    /// </summary>
    public class WorkflowNodeDescription : IVLNodeDescription
    {
        private readonly WorkflowDetail _workflow;
        private readonly IVLNodeDescriptionFactory _factory;

        // Standard pin names
        public const string TriggerInputName = "Trigger";
        public const string CancelInputName = "Cancel";
        public const string AutoRunInputName = "AutoRun";
        public const string RestartOnChangeInputName = "RestartOnChange";
        public const string IsRunningOutputName = "IsRunning";
        public const string ErrorOutputName = "Error";
        public const string DebugOutputName = "Debug";
        public const string InputSchemaJsonOutputName = "InputSchemaJson";
        public const string OutputSchemaJsonOutputName = "OutputSchemaJson";

        public WorkflowNodeDescription(
            WorkflowDetail workflow,
            string vlNodeName,
            string category,
            IVLNodeDescriptionFactory factory)
        {
            _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));

            Name = vlNodeName;
            Category = category;

            // Create comprehensive node summary
            var title = !string.IsNullOrWhiteSpace(workflow.Name) ? workflow.Name : "Nodetool Workflow";
            var description = !string.IsNullOrWhiteSpace(workflow.Description) ? workflow.Description : "";

            if (!string.IsNullOrWhiteSpace(description))
            {
                Summary = TextCleanup.StripTrailingPeriodsPerLine(description);
            }
            else
            {
                Summary = $"Execute {title} workflow";
            }

            // Build comprehensive remarks
            Remarks = BuildWorkflowRemarks();

            // Create input pin descriptions
            var inputPins = new List<IVLPinDescription>();

            // Add trigger pin
            inputPins.Add(new PinDescription(TriggerInputName, typeof(bool), false,
                "🚀 Trigger workflow execution on rising edge",
                "Boolean input - set to true to execute the Nodetool workflow"));

            inputPins.Add(new PinDescription(CancelInputName, typeof(bool), false,
                "🛑 Cancel execution",
                "Boolean input - set to true (rising edge) to cancel the current execution.\n\n"
                + "- If the workflow is not running, this does nothing.\n"
                + "- Cancellation is best-effort: the server may take a moment to stop.\n"
                + "- Output pins keep their last values."));

            inputPins.Add(new PinDescription(AutoRunInputName, typeof(bool), false,
                "🔁 Execute on input change",
                "When enabled, this workflow automatically executes whenever any *workflow input pin* changes.\n\n"
                + "- This watches all workflow input pins (not Trigger/Cancel/AutoRun/RestartOnChange).\n"
                + "- Useful for chaining workflows and building autorun patches.\n"
                + "- If an input changes while a run is active, behavior depends on RestartOnChange."));

            inputPins.Add(new PinDescription(RestartOnChangeInputName, typeof(bool), false,
                "♻️ Restart on input change",
                "Only relevant when AutoRun is enabled.\n\n"
                + "If true and inputs change while the workflow is already running:\n"
                + "- the current run is cancelled, and\n"
                + "- the workflow restarts immediately with the latest inputs.\n\n"
                + "If false:\n"
                + "- the workflow finishes the current run, then reruns once.\n\n"
                + "Tip: enable this for interactive tweaking. Leave it off when the workflow is expensive or you prefer stable completion."));

            // Add workflow input pins
            foreach (var property in _workflow.GetInputProperties())
            {
                var summary = property.Description ?? property.Name ?? "Workflow input";
                var remarks = BuildInputRemarks(property);
                
                // Get consistent VL type and default value
                var (vlType, typeDefault) = GetVLTypeAndDefault(property.Type.Type);
                var defaultValue = property.DefaultValue != null 
                    ? ConvertToVLType(property.DefaultValue, vlType) 
                    : typeDefault;

                inputPins.Add(new PinDescription(property.Name ?? "UnknownInput", vlType, defaultValue, summary, remarks));
            }

            Inputs = inputPins.AsReadOnly();

            // Create output pin descriptions
            var outputPins = new List<IVLPinDescription>();

            // Add standard output pins
            outputPins.Add(new PinDescription(IsRunningOutputName, typeof(bool), false,
                "⏳ Execution status",
                "True while the workflow is processing, false when complete or idle"));

            outputPins.Add(new PinDescription(ErrorOutputName, typeof(string), "",
                "❌ Error message",
                "Contains error details if execution fails, empty string if successful"));

            outputPins.Add(new PinDescription(DebugOutputName, typeof(string), "",
                "🪵 Debug (last updates)",
                "Last few workflow runner updates (progress/node_update/output_update). Useful when results are partial or missing.",
                isVisible: false));

            outputPins.Add(new PinDescription(InputSchemaJsonOutputName, typeof(string), "",
                "📄 Input schema (JSON)",
                "The workflow input_schema as JSON (for debugging/type inspection).",
                isVisible: false));

            outputPins.Add(new PinDescription(OutputSchemaJsonOutputName, typeof(string), "",
                "📄 Output schema (JSON)",
                "The workflow output_schema as JSON (for debugging/type inspection).",
                isVisible: false));

            // Add workflow output pins
            foreach (var property in _workflow.GetOutputProperties())
            {
                var summary = $"📤 {property.Name}";
                var remarks = BuildOutputRemarks(property);
                
                // Get consistent VL type and default value
                var (vlType, defaultValue) = GetVLTypeAndDefault(property.Type.Type);

                outputPins.Add(new PinDescription(property.Name, vlType, defaultValue, summary, remarks));
            }

            Outputs = outputPins.AsReadOnly();
        }

        public string Name { get; }
        public string Category { get; }
        public bool Fragmented => false;
        public IReadOnlyList<IVLPinDescription> Inputs { get; }
        public IReadOnlyList<IVLPinDescription> Outputs { get; }
        public string Summary { get; }
        public string Remarks { get; }
        public IReadOnlyList<string> Tags
        {
            get
            {
                var tags = new List<string> { "Nodetool", "Workflow" };
                if (_workflow.Tags != null)
                {
                    foreach (var t in _workflow.Tags)
                    {
                        if (string.IsNullOrWhiteSpace(t))
                            continue;
                        var trimmed = t.Trim();
                        if (trimmed.Length == 0)
                            continue;
                        if (!tags.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                            tags.Add(trimmed);
                    }
                }
                return tags.AsReadOnly();
            }
        }
        public IVLNodeDescriptionFactory Factory => _factory;
        public IEnumerable<Message> Messages => Enumerable.Empty<Message>();

        // Observable for invalidation
        private readonly Subject<object> _invalidated = new Subject<object>();
        public IObservable<object> Invalidated => _invalidated;

        public IVLNode CreateInstance(NodeContext nodeContext)
        {
            return new WorkflowNodeBase(nodeContext, this, _workflow);
        }

        public IVLNodeDescription? Update(object? updateContext)
        {
            // For now, return this unchanged
            // In a full implementation, this would handle workflow updates
            return this;
        }

        private string BuildWorkflowRemarks()
        {
            static string TrimTrailingPeriod(string s)
                => s.EndsWith(".", StringComparison.Ordinal) ? s.TrimEnd('.') : s;

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(_workflow.Id))
                parts.Add(TrimTrailingPeriod(_workflow.Id.Trim()));
            // Don't repeat Name/Description here: vvvv shows Summary + Remarks, and Summary already contains the
            // human-readable description/title.

            parts.Add($"Created: {_workflow.CreatedAt:yyyy-MM-dd}");
            parts.Add($"Updated: {_workflow.UpdatedAt:yyyy-MM-dd}");

            if (_workflow.Tags != null && _workflow.Tags.Count > 0)
            {
                var shown = _workflow.Tags
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t.Trim())
                    .Where(t => t.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(12)
                    .ToList();
                if (shown.Count > 0)
                    parts.Add($"Tags: {string.Join(", ", shown)}");
            }

            return string.Join("\n", parts);
        }

        private static string BuildInputRemarks((string Name, Nodetool.SDK.Types.TypeMetadata Type, string Description, object? DefaultValue) property)
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(property.Type.Type))
                parts.Add($"Type: {property.Type.Type}");

            if (property.DefaultValue != null)
                parts.Add($"Default: {property.DefaultValue}");

            if (property.Type.Optional)
                parts.Add("(Optional)");

            parts.Add("Workflow input");

            return string.Join(" | ", parts);
        }

        private static string BuildOutputRemarks((string Name, Nodetool.SDK.Types.TypeMetadata Type, string Description) property)
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(property.Type.Type))
                parts.Add($"Type: {property.Type.Type}");

            if (property.Type.Optional)
                parts.Add("(Optional)");

            parts.Add("Workflow output");

            return string.Join(" | ", parts);
        }

        /// <summary>
        /// Get VL type and default value that are consistent with each other
        /// </summary>
        private static (Type, object) GetVLTypeAndDefault(string? type)
        {
            return type?.ToLowerInvariant() switch
            {
                "string" or "str" => (typeof(string), ""),
                "int" or "integer" => (typeof(int), 0),
                "float" or "number" => (typeof(float), 0.0f),
                "bool" or "boolean" => (typeof(bool), false),
                "list" or "array" => (typeof(string[]), new string[0]),
                "any" => (typeof(object), null!),
                "image" => (typeof(SKImage), null!),
                _ => (typeof(string), "")
            };
        }

        /// <summary>
        /// Convert a value to the specified VL type to prevent casting exceptions
        /// </summary>
        private static object ConvertToVLType(object? value, Type targetType)
        {
            // Important: numbers often come through as JsonElement from System.Text.Json.
            // Centralize conversion so defaults work for numeric pins.
            return VlValueConversion.ConvertOrFallback(value, targetType, GetDefaultValueForVLType(targetType))
                   ?? GetDefaultValueForVLType(targetType);
        }

        private static object GetDefaultValueForVLType(Type vlType)
        {
            if (vlType == typeof(string)) return "";
            if (vlType == typeof(int)) return 0;
            if (vlType == typeof(float)) return 0.0f;
            if (vlType == typeof(bool)) return false;
            if (vlType == typeof(string[])) return Array.Empty<string>();
            if (vlType == typeof(SKImage)) return null!;

            try
            {
                return Activator.CreateInstance(vlType) ?? null!;
            }
            catch
            {
                return null!;
            }
        }

        /// <summary>
        /// Internal pin description implementation
        /// </summary>
        private class PinDescription : IVLPinDescription, IVLPinDescriptionWithVisibility
        {
            public PinDescription(string name, Type type, object? defaultValue = null, string summary = "", string remarks = "", bool isVisible = true)
            {
                Name = name;
                Type = type;
                DefaultValue = defaultValue;
                Summary = summary;
                Remarks = remarks;
                IsVisible = isVisible;
                Tags = new List<string>().AsReadOnly();
            }

            public string Name { get; }
            public Type Type { get; }
            public object? DefaultValue { get; }
            public string Summary { get; }
            public string Remarks { get; }
            public IReadOnlyList<string> Tags { get; }
            public bool IsVisible { get; }
        }
    }
} 