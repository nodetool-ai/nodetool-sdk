using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using VL.Core;
using VL.Core.CompilerServices;
using VL.Core.Diagnostics;
using Nodetool.SDK.VL.Models;
using Nodetool.SDK.VL.Streaming;
using Nodetool.SDK.VL.Utilities;
using SkiaSharp;
using VL.Lib.Basics.Audio;

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
        public const string RunInputName = "Run";
        public const string CancelInputName = "Cancel";
        public const string AutoRunInputName = "AutoRun";
        public const string RestartOnChangeInputName = "RestartOnChange";
        public const string ExecutionTimeoutSecondsInputName = "ExecutionTimeoutSeconds";
        public const string IsRunningOutputName = "IsRunning";
        public const string ExecutionTimeOutputName = "Execution Time";
        public const string ErrorOutputName = "Error";
        public const string DebugOutputName = "Debug";

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
                Summary = $"Run {title} workflow";
            }

            // Build comprehensive remarks
            Remarks = BuildWorkflowRemarks();

            // Create input pin descriptions
            var inputPins = new List<IVLPinDescription>();

            // Add run pin
            inputPins.Add(new PinDescription(RunInputName, typeof(bool), false,
                "🚀 Run workflow on rising edge",
                "Boolean input - set to true to run the Nodetool workflow"));

            inputPins.Add(new PinDescription(CancelInputName, typeof(bool), false,
                "🛑 Cancel execution",
                "Boolean input - set to true (rising edge) to cancel the current execution.\n\n"
                + "- If the workflow is not running, this does nothing.\n"
                + "- Cancellation is best-effort: the server may take a moment to stop.\n"
                + "- Output pins keep their last values.",
                isVisible: ExecutionPinVisibility.IsInputVisible(CancelInputName)));

            inputPins.Add(new PinDescription(AutoRunInputName, typeof(bool), false,
                "🔁 Run on input change",
                "When enabled, this workflow automatically executes whenever any *workflow input pin* changes.\n\n"
                + "- This watches workflow data pins (not execution-control pins).\n"
                + "- Useful for chaining workflows and building autorun patches.\n"
                + "- If an input changes while a run is active, behavior depends on RestartOnChange.",
                isVisible: ExecutionPinVisibility.IsInputVisible(AutoRunInputName)));

            inputPins.Add(new PinDescription(RestartOnChangeInputName, typeof(bool), false,
                "♻️ Restart on input change",
                "Only relevant when AutoRun is enabled.\n\n"
                + "If true and inputs change while the workflow is already running:\n"
                + "- the current run is cancelled, and\n"
                + "- the workflow restarts immediately with the latest inputs.\n\n"
                + "If false:\n"
                + "- the workflow finishes the current run, then reruns once.\n\n"
                + "Tip: enable this for interactive tweaking. Leave it off when the workflow is expensive or you prefer stable completion.",
                isVisible: ExecutionPinVisibility.IsInputVisible(RestartOnChangeInputName)));

            inputPins.Add(new PinDescription(ExecutionTimeoutSecondsInputName, typeof(int), 0,
                "Execution timeout override",
                "Maximum duration of this workflow run in seconds. Use 0 to inherit the default from the Nodetool Connect node.",
                isVisible: ExecutionPinVisibility.IsInputVisible(ExecutionTimeoutSecondsInputName)));

            // Add workflow input pins
            foreach (var property in _workflow.GetInputProperties())
            {
                var summary = property.Description ?? property.Name ?? "Workflow input";
                var remarks = BuildInputRemarks(property);
                
                // Get consistent VL type and default value
                var (vlType, typeDefault) = WorkflowVlTypeMapping.GetInputTypeAndDefault(property.Type);
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

            outputPins.Add(new PinDescription(
                ExecutionTimeOutputName,
                typeof(TimeSpan),
                TimeSpan.Zero,
                "Last execution time",
                "End-to-end elapsed time of the last finished workflow run, measured by the SDK client.",
                isVisible: ExecutionPinVisibility.IsOutputVisible(
                    ExecutionTimeOutputName)));

            outputPins.Add(new PinDescription(ErrorOutputName, typeof(string), "",
                "❌ Error message",
                "Contains error details if execution fails, empty string if successful",
                isVisible: ExecutionPinVisibility.IsOutputVisible(ErrorOutputName)));

            outputPins.Add(new PinDescription(DebugOutputName, typeof(string), "",
                "🪵 Debug (last updates)",
                "Last few workflow runner updates (progress/node_update/output_update). Useful when results are partial or missing.",
                isVisible: false));

            // Add workflow output pins
            foreach (var property in _workflow.GetOutputProperties())
            {
                var summary = $"📤 {property.Name}";
                var remarks = BuildOutputRemarks(property);
                
                // Get consistent VL type and default value
                var (vlType, defaultValue) = WorkflowVlTypeMapping.GetTypeAndDefault(property.Type);

                outputPins.Add(new PinDescription(property.Name, vlType, defaultValue, summary, remarks));
            }
            foreach (var audioPin in WorkflowAudioSourcePins.Create(
                         _workflow.Descriptor,
                         outputPins.Select(pin => pin.Name)))
            {
                outputPins.Add(new PinDescription(
                    audioPin.PinName,
                    typeof(IAudioSource),
                    null,
                    $"Realtime audio source for {audioPin.Output.Name}",
                    "Streamed-audio adapter. Connect it to VL.Audio's AudioSourceToAudioSignal node."));
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
                return tags.AsReadOnly();
            }
        }
        public IVLNodeDescriptionFactory Factory => _factory;
        public IEnumerable<Message> Messages => Enumerable.Empty<Message>();

        // Observable for invalidation
        private readonly Subject<object> _invalidated = new Subject<object>();
        private readonly object _invalidationLock = new();
        public IObservable<object> Invalidated => _invalidated;

        internal void Invalidate()
        {
            // A workflow description can have multiple live instances. Serialize their
            // asynchronous invalidations before notifying VL's shared observable.
            lock (_invalidationLock)
                _invalidated.OnNext(this);
        }

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

            return string.Join("\n", parts);
        }

        private static string BuildInputRemarks((
            string Name,
            Nodetool.SDK.Types.TypeMetadata Type,
            string Description,
            object? DefaultValue,
            bool Required,
            double? Minimum,
            double? Maximum) property)
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(property.Type.Type))
                parts.Add($"Type: {property.Type.Type}");

            if (property.DefaultValue != null)
                parts.Add($"Default: {property.DefaultValue}");

            parts.Add(property.Required ? "Required" : "Optional");

            if (property.Minimum.HasValue)
                parts.Add($"Min: {property.Minimum.Value}");

            if (property.Maximum.HasValue)
                parts.Add($"Max: {property.Maximum.Value}");

            if (property.Type.Values is { Count: > 0 })
                parts.Add($"Values: {string.Join(", ", property.Type.Values)}");

            if (WorkflowVlTypeMapping.UsesObjectFallback(property.Type))
                parts.Add("VL fallback: Object");

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

            if (WorkflowVlTypeMapping.UsesObjectFallback(property.Type))
                parts.Add("VL fallback: Object");

            parts.Add("Workflow output");

            return string.Join(" | ", parts);
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
            if (VlValueConversion.IsSpreadType(vlType))
                return VlValueConversion.CreateEmptySpread(vlType.GetGenericArguments()[0]);
            if (vlType.IsArray && vlType.GetElementType() is Type elementType)
                return Array.CreateInstance(elementType, 0);
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
