using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using VL.Core;
using VL.Core.CompilerServices;
using VL.Core.Diagnostics;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Execution;
using Nodetool.SDK.Values;
using Nodetool.SDK.VL.Services;

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

        private readonly object _lock = new();
        private readonly Dictionary<string, StringBuilder> _chunkBuffers = new(StringComparer.Ordinal);
        private readonly Queue<string> _debugLines = new();
        private const int DebugMaxLines = 30;
        
        // Execution state
        private bool _isRunning = false;
        private string _lastError = "";
        private readonly Dictionary<string, NodeToolValue> _lastOutputs = new(StringComparer.Ordinal);
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
            _outputPins["Debug"] = new InternalPin("Debug", typeof(string), "");

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
            lock (_lock)
            {
                _isRunning = true;
                _lastError = "";
                _lastOutputs.Clear();
                _chunkBuffers.Clear();
                _debugLines.Clear();
            }

            try
            {
                AppendDebug($"start node='{_nodeMetadata.NodeType}'");
                // Help debug "changes not taking effect": print the actual loaded DLL + version at runtime.
                var asm = typeof(NodeBase).Assembly;
                Console.WriteLine($"NodeBase: Using assembly '{asm.Location}', version={asm.GetName().Version}");

                if (string.IsNullOrWhiteSpace(_nodeMetadata.NodeType))
                    throw new InvalidOperationException("NodeType is missing from node metadata.");

                // Ensure we have a connected client (user can also do this explicitly via the Connect node)
                if (!NodeToolClientProvider.IsConnected)
                {
                    var connected = await NodeToolClientProvider.ConnectAsync();
                    if (!connected)
                        throw new InvalidOperationException($"Not connected: {NodeToolClientProvider.LastError ?? "unknown error"}");
                }
                AppendDebug("connected");

                var client = NodeToolClientProvider.GetClient();

                // Collect input values
                var inputData = new Dictionary<string, object>(StringComparer.Ordinal);
                
                if (_nodeMetadata.Properties != null)
                {
                    foreach (var property in _nodeMetadata.Properties)
                    {
                        if (_inputPins.TryGetValue(property.Name, out var inputPin))
                        {
                            inputData[property.Name] = inputPin.Value ?? property.Default ?? "";
                        }
                        else
                        {
                            inputData[property.Name] = property.Default ?? "";
                        }
                    }
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(300));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);

                IExecutionSession? session = null;
                try
                {
                    session = await client.ExecuteNodeAsync(_nodeMetadata.NodeType, inputData, linked.Token);

                    session.ProgressChanged += p => AppendDebug($"progress={(p * 100):0}%");
                    session.OutputReceived += update =>
                    {
                        if (string.IsNullOrWhiteSpace(update.OutputName))
                            return;

                        AppendDebug($"output_update: {update.OutputName} type={update.OutputType}");
                        lock (_lock)
                        {
                            // Chunk streaming: turn {type:"chunk", content:"..."} into accumulated text.
                            if (update.Value.Kind == NodeToolValueKind.Map &&
                                string.Equals(update.Value.TypeDiscriminator, "chunk", StringComparison.OrdinalIgnoreCase))
                            {
                                var map = update.Value.AsMapOrEmpty();
                                var content = map.TryGetValue("content", out var c) ? (c.AsString() ?? "") : "";

                                if (!_chunkBuffers.TryGetValue(update.OutputName, out var sb))
                                {
                                    sb = new StringBuilder();
                                    _chunkBuffers[update.OutputName] = sb;
                                }

                                if (!string.IsNullOrEmpty(content))
                                    sb.Append(content);

                                _lastOutputs[update.OutputName] = NodeToolValue.From(sb.ToString());
                                return;
                            }

                            _lastOutputs[update.OutputName] = update.Value;
                        }
                    };

                    session.NodeUpdated += update =>
                    {
                        if (!string.IsNullOrWhiteSpace(update.error))
                        {
                            lock (_lock)
                            {
                                _lastError = update.error ?? "";
                            }
                            AppendDebug($"node_error: {update.error}");
                        }

                        if (update.result != null)
                        {
                            lock (_lock)
                            {
                                foreach (var kvp in update.result)
                                {
                                    if (string.IsNullOrWhiteSpace(kvp.Key))
                                        continue;
                                    _lastOutputs[kvp.Key] = NodeToolValue.From(kvp.Value);
                                }
                            }
                        }
                    };

                    var ok = await session.WaitForCompletionAsync(linked.Token);
                    if (!ok)
                    {
                        lock (_lock)
                        {
                            _lastError = session.ErrorMessage ?? _lastError;
                        }
                        AppendDebug($"completed: failed err='{session.ErrorMessage ?? _lastError}'");
                    }
                    else
                    {
                        AppendDebug("completed: ok");
                    }
                }
                finally
                {
                    session?.Dispose();
                }
            }
            catch (Exception ex)
            {
                lock (_lock)
                {
                    _lastError = $"Execution error: {ex.Message}";
                    _lastOutputs.Clear();
                }
                AppendDebug($"exception: {ex.Message}");
            }
            finally
            {
                lock (_lock)
                {
                    _isRunning = false;
                }
                AppendDebug("done");
            }
        }

        /// <summary>
        /// Update output pin values
        /// </summary>
        private void UpdateOutputs()
        {
            try
            {
                bool isRunning;
                string lastError;
                Dictionary<string, NodeToolValue> outputsSnapshot;
                string debugText;

                lock (_lock)
                {
                    isRunning = _isRunning;
                    lastError = _lastError;
                    outputsSnapshot = new Dictionary<string, NodeToolValue>(_lastOutputs, StringComparer.Ordinal);
                    debugText = string.Join(Environment.NewLine, _debugLines);
                }

                // Set standard outputs
                if (_outputPins.TryGetValue("IsRunning", out var isRunningPin))
                    isRunningPin.Value = isRunning;
                if (_outputPins.TryGetValue("Error", out var errorPin))
                    errorPin.Value = lastError;
                if (_outputPins.TryGetValue("Debug", out var debugPin))
                    debugPin.Value = debugText;

                // Set node-specific outputs - ensure type safety
                if (_nodeMetadata.Outputs != null)
                {
                    foreach (var output in _nodeMetadata.Outputs)
                    {
                        if (_outputPins.TryGetValue(output.Name, out var outputPin))
                        {
                            // Get the expected VL type from the node metadata
                            var (expectedType, defaultValue) = MapNodeType(output.Type);
                            object? valueToSet;
                            
                            if (outputsSnapshot.TryGetValue(output.Name, out var value))
                            {
                                valueToSet = ConvertNodeToolValueToExpectedType(value, expectedType ?? typeof(string));
                            }
                            else
                            {
                                // Set default value if no output available
                                valueToSet = defaultValue;
                            }
                            
                            outputPin.Value = valueToSet;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lock (_lock)
                {
                    _lastError = $"Output update error: {ex.Message}";
                }
            }
        }

        private void AppendDebug(string line)
        {
            lock (_lock)
            {
                var ts = DateTime.Now.ToString("HH:mm:ss.fff");
                var msg = $"{ts} {line}";
                while (_debugLines.Count >= DebugMaxLines)
                    _debugLines.Dequeue();
                _debugLines.Enqueue(msg);
            }
        }

        /// <summary>
        /// Get default value for a given type - must match MapNodeType exactly
        /// </summary>
        private static object? GetDefaultValueForType(NodeTypeDefinition? nodeType)
        {
            // Use the same mapping as MapNodeType to ensure consistency
            var (vlType, defaultValue) = MapNodeType(nodeType);
            return defaultValue;
        }

        /// <summary>
        /// Get default value for a specific .NET type (for VL pins)
        /// </summary>
        private static object? GetDefaultValueForPinType(Type pinType)
        {
            if (pinType == typeof(string)) return "";
            if (pinType == typeof(int)) return 0;
            if (pinType == typeof(float)) return 0.0f;
            if (pinType == typeof(bool)) return false;
            if (pinType == typeof(string[])) return new string[0];
            if (pinType == typeof(object)) return null;
            
            // For other types, try to create an instance
            try
            {
                return Activator.CreateInstance(pinType);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Convert a NodeToolValue to the expected pin type to prevent casting exceptions
        /// </summary>
        private static object? ConvertNodeToolValueToExpectedType(NodeToolValue value, Type expectedType)
        {
            // Fast paths for common VL pin types
            try
            {
                if (expectedType == typeof(string))
                {
                    // Avoid ToString() on dictionaries (it yields "System.Collections.Generic.Dictionary`2[...]").
                    // For refs/typed payloads, try common fields first, otherwise fall back to JSON.
                    if (value.Kind == NodeToolValueKind.Map)
                    {
                        var map = value.AsMapOrEmpty();

                        if (map.TryGetValue("uri", out var uri) &&
                            uri.AsString() is string uriStr &&
                            !string.IsNullOrWhiteSpace(uriStr))
                        {
                            return uriStr;
                        }

                        // Common payload shapes:
                        // - { type: "text", text: "..." }
                        // - { type: "chunk", content: "..." }
                        // - { value: "..." }
                        // - { result: "..." }
                        if (map.TryGetValue("text", out var textVal) && textVal.AsString() is string textStr)
                            return textStr;

                        if (map.TryGetValue("content", out var contentVal))
                            return contentVal.AsString() ?? contentVal.ToJsonString();

                        if (map.TryGetValue("value", out var valueVal))
                            return valueVal.AsString() ?? valueVal.ToJsonString();

                        if (map.TryGetValue("result", out var resultVal))
                            return resultVal.AsString() ?? resultVal.ToJsonString();

                        return value.ToJsonString();
                    }

                    if (value.Kind == NodeToolValueKind.List)
                        return value.ToJsonString();

                    return value.AsString() ?? value.ToJsonString();
                }
                else if (expectedType == typeof(int))
                {
                    if (value.TryGetLong(out var l))
                        return (int)l;
                    return 0;
                }
                else if (expectedType == typeof(float))
                {
                    if (value.TryGetDouble(out var d))
                        return (float)d;
                    return 0.0f;
                }
                else if (expectedType == typeof(bool))
                {
                    if (value.TryGetBool(out var b))
                        return b;
                    return false;
                }
                else if (expectedType == typeof(string[]))
                {
                    if (value.Kind == NodeToolValueKind.List)
                        return value.AsListOrEmpty().Select(v => v.AsString() ?? v.ToJsonString()).ToArray();
                    return new[] { value.AsString() ?? value.ToJsonString() };
                }
                else if (expectedType == typeof(object))
                {
                    // VL isn't great at displaying/handling arbitrary dictionaries; prefer readable text/JSON.
                    if (value.Kind == NodeToolValueKind.Map)
                    {
                        var map = value.AsMapOrEmpty();
                        if (map.TryGetValue("text", out var textVal) && textVal.AsString() is string textStr)
                            return textStr;
                        if (map.TryGetValue("delta", out var deltaVal))
                            return deltaVal.AsString() ?? deltaVal.ToJsonString();
                        if (map.TryGetValue("content", out var contentVal))
                            return contentVal.AsString() ?? contentVal.ToJsonString();
                        return value.ToJsonString();
                    }
                    if (value.Kind == NodeToolValueKind.List)
                        return value.ToJsonString();

                    return value.Raw; // primitives etc.
                }
                else
                {
                    return value.Raw ?? value.AsString() ?? value.ToJsonString();
                }
            }
            catch (Exception)
            {
                // If conversion fails, return default value for the expected type
                return GetDefaultValueForPinType(expectedType);
            }
        }

        /// <summary>
        /// Dispose resources when node is removed
        /// </summary>
        public void Dispose()
        {
            // Clean up any resources
            lock (_lock)
            {
                _lastOutputs.Clear();
            }
        }

        /// <summary>
        /// Map Nodetool type to VL type - must match NodesFactory.MapNodeType exactly
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