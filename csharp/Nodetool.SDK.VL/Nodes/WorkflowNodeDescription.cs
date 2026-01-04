using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using VL.Core;
using VL.Core.CompilerServices;
using VL.Core.Diagnostics;
using Nodetool.SDK.VL.Models;
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
        public const string IsRunningOutputName = "IsRunning";
        public const string ErrorOutputName = "Error";

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
                Summary = description;
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
        public IReadOnlyList<string> Tags => new List<string> { "Nodetool", "Workflow" }.AsReadOnly();
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
            var parts = new List<string>();
            parts.Add($"Nodetool Workflow ID: {_workflow.Id}");
            parts.Add($"Name: {_workflow.Name}");

            if (!string.IsNullOrWhiteSpace(_workflow.Description) &&
                _workflow.Description != _workflow.Name)
                parts.Add($"Description: {_workflow.Description}");

            var inputCount = _workflow.GetInputProperties().Count();
            var outputCount = _workflow.GetOutputProperties().Count();
            parts.Add($"📌 {inputCount} inputs, {outputCount} outputs");

            parts.Add($"Created: {_workflow.CreatedAt:yyyy-MM-dd}");
            parts.Add($"Updated: {_workflow.UpdatedAt:yyyy-MM-dd}");

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
            if (value == null)
            {
                // Return a type-correct default for the VL pin.
                // Important: for reference types like SKImage, this must be null (not "").
                return GetDefaultValueForVLType(targetType);
            }

            if (targetType.IsAssignableFrom(value.GetType()))
                return value;

            try
            {
                if (targetType == typeof(string))
                {
                    return value.ToString() ?? "";
                }
                else if (targetType == typeof(int))
                {
                    return Convert.ToInt32(value);
                }
                else if (targetType == typeof(float))
                {
                    return Convert.ToSingle(value);
                }
                else if (targetType == typeof(bool))
                {
                    return Convert.ToBoolean(value);
                }
                else if (targetType == typeof(string[]))
                {
                    if (value is Array array)
                    {
                        var stringArray = new string[array.Length];
                        for (int i = 0; i < array.Length; i++)
                        {
                            stringArray[i] = array.GetValue(i)?.ToString() ?? "";
                        }
                        return stringArray;
                    }
                    else
                    {
                        return new string[] { value.ToString() ?? "" };
                    }
                }
                else
                {
                    return Convert.ChangeType(value, targetType);
                }
            }
            catch
            {
                // If conversion fails, return default value for the target type
                return GetDefaultValueForVLType(targetType);
            }
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
        private class PinDescription : IVLPinDescription
        {
            public PinDescription(string name, Type type, object? defaultValue = null, string summary = "", string remarks = "")
            {
                Name = name;
                Type = type;
                DefaultValue = defaultValue;
                Summary = summary;
                Remarks = remarks;
                Tags = new List<string>().AsReadOnly();
            }

            public string Name { get; }
            public Type Type { get; }
            public object? DefaultValue { get; }
            public string Summary { get; }
            public string Remarks { get; }
            public IReadOnlyList<string> Tags { get; }
        }
    }
} 