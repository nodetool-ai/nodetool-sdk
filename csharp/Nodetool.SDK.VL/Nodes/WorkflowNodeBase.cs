using System;
using System.Collections.Generic;
using System.Linq;
using VL.Core;
using Nodetool.SDK.VL.Models;

namespace Nodetool.SDK.VL.Nodes
{
    /// <summary>
    /// Base class for Nodetool workflow nodes in VL
    /// </summary>
    public class WorkflowNodeBase : IVLNode, IDisposable
    {
        private readonly NodeContext _nodeContext;
        private readonly WorkflowDetail _workflow;
        private readonly WorkflowNodeDescription _description;
        private readonly Dictionary<string, IVLPin> _inputPins;
        private readonly Dictionary<string, IVLPin> _outputPins;

        private bool _lastTriggerState = false;
        private bool _isDisposed = false;

        public WorkflowNodeBase(NodeContext nodeContext, WorkflowNodeDescription description, WorkflowDetail workflow)
        {
            _nodeContext = nodeContext ?? throw new ArgumentNullException(nameof(nodeContext));
            _description = description ?? throw new ArgumentNullException(nameof(description));
            _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
            
            // Create input pins
            _inputPins = new Dictionary<string, IVLPin>();
            
            // Add trigger pin
            _inputPins["Trigger"] = new InternalPin("Trigger", typeof(bool), false);
            
            // Add workflow input pins
            foreach (var property in _workflow.GetInputProperties())
            {
                // Get consistent VL type and default value
                var (vlType, typeDefault) = GetVLTypeAndDefault(property.Type.Type);
                var defaultValue = property.DefaultValue != null 
                    ? ConvertToExpectedType(property.DefaultValue, vlType) 
                    : typeDefault;
                _inputPins[property.Name] = new InternalPin(property.Name, vlType, defaultValue);
            }
            
            // Create output pins
            _outputPins = new Dictionary<string, IVLPin>();
            
            // Add standard output pins
            _outputPins["IsRunning"] = new InternalPin("IsRunning", typeof(bool), false);
            _outputPins["Error"] = new InternalPin("Error", typeof(string), "");
            
            // Add workflow output pins
            foreach (var property in _workflow.GetOutputProperties())
            {
                // Get consistent VL type and default value
                var (vlType, defaultValue) = GetVLTypeAndDefault(property.Type.Type);
                _outputPins[property.Name] = new InternalPin(property.Name, vlType, defaultValue);
            }

            Console.WriteLine($"WorkflowNodeBase: Created workflow node '{_workflow.Name}' with {_inputPins.Count} inputs and {_outputPins.Count} outputs");
        }

        public IVLPin[] Inputs => _inputPins.Values.ToArray();
        public IVLPin[] Outputs => _outputPins.Values.ToArray();

        // IVLNode implementation
        public IVLNodeDescription NodeDescription => _description;
        public NodeContext Context => _nodeContext;
        public AppHost AppHost => _nodeContext.AppHost;
        public uint Identity => (uint)_nodeContext.Path.GetHashCode();

        public IVLObject With(IReadOnlyDictionary<string, object> values)
        {
            // For now, return this as we don't support configuration changes
            return this;
        }

        public void Update()
        {
            if (_isDisposed) return;

            try
            {
                // Check for trigger edge (false → true)
                var triggerPin = _inputPins["Trigger"];
                bool currentTriggerState = (bool)(triggerPin.Value ?? false);

                if (currentTriggerState && !_lastTriggerState)
                {
                    // Rising edge detected - execute workflow
                    ExecuteWorkflowAsync();
                }

                _lastTriggerState = currentTriggerState;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WorkflowNodeBase: Error in Update(): {ex.Message}");
                SetError($"Update error: {ex.Message}");
            }
        }

        private async void ExecuteWorkflowAsync()
        {
            try
            {
                Console.WriteLine($"WorkflowNodeBase: Starting execution of workflow '{_workflow.Name}'");
                SetIsRunning(true);
                SetError("");

                // Simulate workflow execution for now
                await System.Threading.Tasks.Task.Delay(100);

                // TODO: Implement real workflow execution here
                // This would involve:
                // 1. Collect input values from pins
                // 2. Call Nodetool API to execute workflow
                // 3. Parse results and set output pins

                Console.WriteLine($"WorkflowNodeBase: Workflow '{_workflow.Name}' execution completed (simulated)");
                SetIsRunning(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WorkflowNodeBase: Error executing workflow '{_workflow.Name}': {ex.Message}");
                SetError($"Execution failed: {ex.Message}");
                SetIsRunning(false);
            }
        }

        private void SetIsRunning(bool isRunning)
        {
            if (_outputPins.TryGetValue("IsRunning", out var pin))
            {
                pin.Value = isRunning;
            }
        }

        private void SetError(string error)
        {
            if (_outputPins.TryGetValue("Error", out var pin))
            {
                pin.Value = error;
            }
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
                _ => (typeof(string), "")
            };
        }

        /// <summary>
        /// Get default value for a specific .NET type to ensure type safety
        /// </summary>
        private static object GetDefaultValueForVLType(Type vlType)
        {
            if (vlType == typeof(string)) return "";
            if (vlType == typeof(int)) return 0;
            if (vlType == typeof(float)) return 0.0f;
            if (vlType == typeof(bool)) return false;
            if (vlType == typeof(string[])) return new string[0];
            
            try
            {
                return Activator.CreateInstance(vlType) ?? "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Convert value to expected type to prevent casting exceptions
        /// </summary>
        private static object ConvertToExpectedType(object? value, Type expectedType)
        {
            if (value == null)
                return GetDefaultValueForVLType(expectedType);

            if (expectedType.IsAssignableFrom(value.GetType()))
                return value;

            try
            {
                if (expectedType == typeof(string))
                    return value.ToString() ?? "";
                else if (expectedType == typeof(int))
                    return Convert.ToInt32(value);
                else if (expectedType == typeof(float))
                    return Convert.ToSingle(value);
                else if (expectedType == typeof(bool))
                    return Convert.ToBoolean(value);
                else if (expectedType == typeof(string[]))
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
                    return Convert.ChangeType(value, expectedType);
            }
            catch
            {
                return GetDefaultValueForVLType(expectedType);
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Console.WriteLine($"WorkflowNodeBase: Disposing workflow node '{_workflow.Name}'");
                
                _isDisposed = true;
            }
        }

        /// <summary>
        /// Internal pin implementation for workflow nodes
        /// </summary>
        public class InternalPin : IVLPin
        {
            public InternalPin(string name, Type type, object? defaultValue)
            {
                Name = name;
                Type = type;
                Value = defaultValue;
            }

            public string Name { get; }
            public Type Type { get; }
            public object? Value { get; set; }
        }
    }
} 