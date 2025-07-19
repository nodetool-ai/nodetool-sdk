using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.CompilerServices;
using VL.Core.Diagnostics;
using Nodetool.SDK.Api.Models;

namespace Nodetool.SDK.VL.Nodes
{
    /// <summary>
    /// Base implementation for individual Nodetool nodes in VL
    /// </summary>
    public class NodeBase : IVLNode
    {
        private readonly NodeContext _nodeContext;
        private readonly NodeMetadataResponse _nodeMetadata;
        private readonly Dictionary<string, IVLPin> _inputPins;
        private readonly Dictionary<string, IVLPin> _outputPins;
        private readonly IVLNodeDescription _nodeDescription;
        
        // Execution state
        private bool _isRunning = false;
        private string _lastError = "";
        private Dictionary<string, object?> _lastOutputs = new Dictionary<string, object?>();
        private bool _lastExecuteState = false;

        public NodeBase(NodeContext nodeContext, NodeMetadataResponse nodeMetadata)
        {
            _nodeContext = nodeContext ?? throw new ArgumentNullException(nameof(nodeContext));
            _nodeMetadata = nodeMetadata ?? throw new ArgumentNullException(nameof(nodeMetadata));

            // Create a minimal node description for VL's requirements
            _nodeDescription = new SimpleNodeDescription(_nodeMetadata);

            // Create pin instances
            _inputPins = new Dictionary<string, IVLPin>();
            _outputPins = new Dictionary<string, IVLPin>();

            // Create input pins - VL's factory already created the pin descriptions
            // Add Execute pin
            _inputPins["Execute"] = new InternalPin("Execute", typeof(bool), false);

            // Add input pins from node properties
            if (_nodeMetadata.Properties != null)
            {
                foreach (var property in _nodeMetadata.Properties)
                {
                    var (vlType, defaultValue) = MapNodeType(property.Type);
                    var pin = new InternalPin(property.Name, vlType ?? typeof(string), defaultValue);
                    _inputPins[property.Name] = pin;
                }
            }

            // Create output pins - VL's factory already created the pin descriptions
            // Add node-specific output pins
            if (_nodeMetadata.Outputs != null)
            {
                foreach (var output in _nodeMetadata.Outputs)
                {
                    var (vlType, defaultValue) = MapNodeType(output.Type);
                    var pin = new InternalPin(output.Name, vlType ?? typeof(string), defaultValue);
                    _outputPins[output.Name] = pin;
                }
            }

            // Add standard status outputs
            _outputPins["IsRunning"] = new InternalPin("IsRunning", typeof(bool), false);
            _outputPins["Error"] = new InternalPin("Error", typeof(string), "");

            // Set initial output states
            if (_outputPins.TryGetValue("IsRunning", out var isRunningPin))
                isRunningPin.Value = false;
            if (_outputPins.TryGetValue("Error", out var errorPin))
                errorPin.Value = string.Empty;
        }

        // IVLNode implementation
        public IVLNodeDescription NodeDescription => _nodeDescription;
        public IVLPin[] Inputs => _inputPins.Values.ToArray();
        public IVLPin[] Outputs => _outputPins.Values.ToArray();

        // IVLObject implementation  
        public NodeContext Context => _nodeContext;
        public AppHost AppHost => _nodeContext.AppHost;
        public uint Identity => (uint)_nodeContext.Path.GetHashCode();

        public IVLObject With(IReadOnlyDictionary<string, object> values)
        {
            // For now, return this without modification
            return this;
        }

        /// <summary>
        /// Update the node - called by VL on each frame
        /// </summary>
        public void Update()
        {
            try
            {
                // Check for Execute trigger on rising edge
                if (_inputPins.TryGetValue("Execute", out var executePin))
                {
                    bool currentExecuteState = executePin.Value is bool b && b;
                    
                    if (currentExecuteState && !_lastExecuteState && !_isRunning)
                    {
                        // Rising edge detected - execute the node
                        _ = ExecuteNodeAsync();
                    }
                    
                    _lastExecuteState = currentExecuteState;
                }

                // Update output pins with current state
                UpdateOutputs();
            }
            catch (Exception ex)
            {
                _lastError = $"Update error: {ex.Message}";
                _isRunning = false;
            }
        }

        /// <summary>
        /// Execute the Nodetool node asynchronously
        /// </summary>
        private async Task ExecuteNodeAsync()
        {
            _isRunning = true;
            _lastError = "";

            try
            {
                // Collect input values
                var inputData = new Dictionary<string, object?>();
                
                if (_nodeMetadata.Properties != null)
                {
                    foreach (var property in _nodeMetadata.Properties)
                    {
                        if (_inputPins.TryGetValue(property.Name, out var inputPin))
                        {
                            inputData[property.Name] = inputPin.Value ?? property.Default;
                        }
                        else
                        {
                            inputData[property.Name] = property.Default;
                        }
                    }
                }

                // TODO: Execute the actual node through Nodetool API
                // For now, simulate execution with a delay
                await Task.Delay(1000);

                // TODO: Parse and store actual results
                // For now, simulate outputs
                _lastOutputs.Clear();
                if (_nodeMetadata.Outputs != null)
                {
                    foreach (var output in _nodeMetadata.Outputs)
                    {
                        // Simulate output based on type
                        _lastOutputs[output.Name] = GetSimulatedOutput(output);
                    }
                }

                _lastError = "";
            }
            catch (Exception ex)
            {
                _lastError = $"Execution error: {ex.Message}";
                _lastOutputs.Clear();
            }
            finally
            {
                _isRunning = false;
            }
        }

        /// <summary>
        /// Get simulated output for testing purposes
        /// </summary>
        private object? GetSimulatedOutput(NodeOutput output)
        {
            if (output.Type == null || string.IsNullOrEmpty(output.Type.Type))
                return "Simulated output";

            return output.Type.Type.ToLowerInvariant() switch
            {
                "str" or "string" => $"Output from {_nodeMetadata.NodeType}",
                "int" or "integer" => 42,
                "float" or "number" => 3.14f,
                "bool" or "boolean" => true,
                "list" or "array" => new string[] { "result1", "result2" },
                "image" => "data:image/png;base64,simulated_image_data",
                "audio" => "simulated_audio_data",
                "video" => "simulated_video_data",
                _ => "Simulated output"
            };
        }

        /// <summary>
        /// Update output pin values
        /// </summary>
        private void UpdateOutputs()
        {
            try
            {
                // Set standard outputs
                if (_outputPins.TryGetValue("IsRunning", out var isRunningPin))
                    isRunningPin.Value = _isRunning;
                if (_outputPins.TryGetValue("Error", out var errorPin))
                    errorPin.Value = _lastError;

                // Set node-specific outputs
                if (_nodeMetadata.Outputs != null)
                {
                    foreach (var output in _nodeMetadata.Outputs)
                    {
                        if (_outputPins.TryGetValue(output.Name, out var outputPin))
                        {
                            if (_lastOutputs.TryGetValue(output.Name, out var value))
                            {
                                outputPin.Value = value;
                            }
                            else
                            {
                                // Set default value if no output available
                                outputPin.Value = GetDefaultValueForType(output.Type);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _lastError = $"Output update error: {ex.Message}";
            }
        }

        /// <summary>
        /// Get default value for a given type
        /// </summary>
        private static object? GetDefaultValueForType(NodeTypeDefinition? nodeType)
        {
            if (nodeType == null || string.IsNullOrEmpty(nodeType.Type))
                return "";

            return nodeType.Type.ToLowerInvariant() switch
            {
                "str" or "string" => "",
                "int" or "integer" => 0,
                "float" or "number" => 0.0f,
                "bool" or "boolean" => false,
                "list" or "array" => new string[0],
                "dict" or "object" => null,
                _ => ""
            };
        }

        /// <summary>
        /// Dispose resources when node is removed
        /// </summary>
        public void Dispose()
        {
            // Clean up any resources
            _lastOutputs.Clear();
        }

        /// <summary>
        /// Map Nodetool type to VL type - simplified version
        /// </summary>
        private static (Type?, object?) MapNodeType(NodeTypeDefinition? nodeType)
        {
            if (nodeType == null || string.IsNullOrEmpty(nodeType.Type))
                return (typeof(string), "");

            return nodeType.Type.ToLowerInvariant() switch
            {
                "str" or "string" => (typeof(string), ""),
                "int" or "integer" => (typeof(int), 0),
                "float" or "number" => (typeof(float), 0.0f),
                "bool" or "boolean" => (typeof(bool), false),
                "list" or "array" => (typeof(string[]), new string[0]),
                "dict" or "object" => (typeof(object), null),
                "image" => (typeof(string), ""), // Image path or base64
                "audio" => (typeof(string), ""), // Audio path or data
                "video" => (typeof(string), ""), // Video path or data
                _ => (typeof(string), "")
            };
        }

        /// <summary>
        /// Internal pin implementation for VL nodes
        /// </summary>
        private class InternalPin : IVLPin
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

        /// <summary>
        /// Minimal node description implementation for VL's requirements
        /// The actual documentation comes from VL's factory pattern, not from this class
        /// </summary>
        private class SimpleNodeDescription : IVLNodeDescription
        {
            private readonly NodeMetadataResponse _nodeMetadata;

            public SimpleNodeDescription(NodeMetadataResponse nodeMetadata)
            {
                _nodeMetadata = nodeMetadata;
                Name = _nodeMetadata.NodeType ?? "Unknown";
                Category = "Nodetool Nodes.General";
                Summary = _nodeMetadata.Description ?? _nodeMetadata.Title ?? Name;
                Remarks = $"NodeTool Type: {_nodeMetadata.NodeType}";
                
                // Create minimal pin descriptions - the factory handles the real documentation
                Inputs = CreateInputDescriptions();
                Outputs = CreateOutputDescriptions();
            }

            public string Name { get; }
            public string Category { get; }
            public bool Fragmented => false;
            public IReadOnlyList<IVLPinDescription> Inputs { get; }
            public IReadOnlyList<IVLPinDescription> Outputs { get; }
            public string Summary { get; }
            public string Remarks { get; }
            public IReadOnlyList<string> Tags => new List<string> { "Nodetool" }.AsReadOnly();
            public IVLNodeDescriptionFactory Factory => null!; // Factory is handled by VL's factory pattern
            public IEnumerable<Message> Messages => Enumerable.Empty<Message>();
            
            private readonly Subject<object> _invalidated = new Subject<object>();
            public IObservable<object> Invalidated => _invalidated;

            public IVLNode CreateInstance(NodeContext nodeContext)
            {
                return new NodeBase(nodeContext, _nodeMetadata);
            }

            public IVLNodeDescription? Update(object? updateContext) => this;

            private IReadOnlyList<IVLPinDescription> CreateInputDescriptions()
            {
                var pins = new List<IVLPinDescription>();
                pins.Add(new SimplePinDescription("Execute", typeof(bool)));
                
                if (_nodeMetadata.Properties != null)
                {
                    foreach (var property in _nodeMetadata.Properties)
                    {
                        var (vlType, _) = MapNodeType(property.Type);
                        pins.Add(new SimplePinDescription(property.Name, vlType ?? typeof(string)));
                    }
                }
                
                return pins.AsReadOnly();
            }

            private IReadOnlyList<IVLPinDescription> CreateOutputDescriptions()
            {
                var pins = new List<IVLPinDescription>();
                
                if (_nodeMetadata.Outputs != null)
                {
                    foreach (var output in _nodeMetadata.Outputs)
                    {
                        var (vlType, _) = MapNodeType(output.Type);
                        pins.Add(new SimplePinDescription(output.Name, vlType ?? typeof(string)));
                    }
                }
                
                pins.Add(new SimplePinDescription("IsRunning", typeof(bool)));
                pins.Add(new SimplePinDescription("Error", typeof(string)));
                
                return pins.AsReadOnly();
            }
        }

        /// <summary>
        /// Minimal pin description for VL's requirements
        /// </summary>
        private class SimplePinDescription : IVLPinDescription
        {
            public SimplePinDescription(string name, Type type)
            {
                Name = name;
                Type = type;
                DefaultValue = GetDefaultValueForType(type);
            }

            private static object? GetDefaultValueForType(Type type)
            {
                if (type == typeof(string)) return "";
                if (type == typeof(int)) return 0;
                if (type == typeof(float)) return 0.0f;
                if (type == typeof(bool)) return false;
                if (type == typeof(string[])) return new string[0];
                if (type == typeof(object)) return null;
                return Activator.CreateInstance(type);
            }

            public string Name { get; }
            public Type Type { get; }
            public object? DefaultValue { get; }
            public string Summary => "";
            public string Remarks => "";
            public IReadOnlyList<string> Tags => new List<string>().AsReadOnly();
        }
    }
} 