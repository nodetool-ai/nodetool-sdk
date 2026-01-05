using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
using Nodetool.SDK.VL.Utilities;

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

        // Auto-run on input change
        private bool _autoRunEnabled = false;
        private bool _restartOnChangeEnabled = false;
        private string _lastInputSignature = "";
        private bool _rerunRequested = false;

        private bool _cancelRequestedByRestart = false;
        private IExecutionSession? _activeSession = null;
        private CancellationTokenSource? _manualCancelCts = null;

        // VL evaluation can be demand-driven; without invalidation pulses, async state changes might not propagate.
        private long _lastInvalidateTicks = 0;

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
            _inputPins["AutoRun"] = new InternalPin("AutoRun", typeof(bool), false);
            _inputPins["RestartOnChange"] = new InternalPin("RestartOnChange", typeof(bool), false);

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
                        // Keep auto-run signature in sync to avoid immediate duplicate run.
                        _lastInputSignature = ComputeInputSignature();
                        _ = ExecuteNodeAsync();
                    }
                    
                    _lastExecuteState = currentExecuteState;
                }

                // Auto-run toggle
                if (_inputPins.TryGetValue("AutoRun", out var autoRunPin))
                {
                    _autoRunEnabled = autoRunPin.Value is bool b && b;
                }

                if (_inputPins.TryGetValue("RestartOnChange", out var restartPin))
                {
                    _restartOnChangeEnabled = restartPin.Value is bool b && b;
                }

                if (_autoRunEnabled)
                {
                    var sig = ComputeInputSignature();
                    if (!string.Equals(sig, _lastInputSignature, StringComparison.Ordinal))
                    {
                        _lastInputSignature = sig;
                        if (_isRunning)
                        {
                            _rerunRequested = true;
                            if (_restartOnChangeEnabled)
                            {
                                _cancelRequestedByRestart = true;
                                _ = CancelActiveRunAsync();
                            }
                        }
                        else
                        {
                            _ = ExecuteNodeAsync();
                        }
                    }
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
                // Keep last outputs latched across runs for better VL ergonomics.
                // (If the next run produces no outputs or fails, downstream patches still have the previous value.)
                _chunkBuffers.Clear();
                _debugLines.Clear();
                _rerunRequested = false;
                _cancelRequestedByRestart = false;

                _manualCancelCts?.Dispose();
                _manualCancelCts = new CancellationTokenSource();
            }

            // Update pins immediately so IsRunning can't get stuck if VL skips subsequent Update() calls.
            SetIsRunning(true);
            SetError("");
            InvalidateOutputs();

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

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(300));
                var localManual = _manualCancelCts;
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, localManual?.Token ?? CancellationToken.None);

                IExecutionSession? session = null;
                // For single-node execution, node_update is often the most reliable completion signal.
                // Some servers may omit job_update in certain edge cases; this prevents IsRunning from hanging.
                var nodeTerminalTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                try
                {
                    session = await client.ExecuteNodeAsync(_nodeMetadata.NodeType, inputData, linked.Token);
                    lock (_lock)
                    {
                        _activeSession = session;
                    }

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
                        InvalidateOutputs();
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
                            SetError(_lastError);
                            InvalidateOutputs();
                            nodeTerminalTcs.TrySetResult(false);
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
                            InvalidateOutputs();
                        }

                        // Terminal node statuses for single-node graphs.
                        // We treat these as completion signals even if job_update is missing.
                        if (IsTerminalNodeStatus(update.status))
                        {
                            var ok = string.IsNullOrWhiteSpace(update.error) &&
                                     string.Equals(update.status, "completed", StringComparison.OrdinalIgnoreCase);
                            AppendDebug($"node_status: {update.status}");
                            nodeTerminalTcs.TrySetResult(ok);
                        }
                    };

                    var completedTask = await Task.WhenAny(
                        session.WaitForCompletionAsync(linked.Token),
                        nodeTerminalTcs.Task);

                    bool ok;
                    if (ReferenceEquals(completedTask, nodeTerminalTcs.Task))
                    {
                        ok = nodeTerminalTcs.Task.Result;
                        if (!ok && string.IsNullOrWhiteSpace(_lastError))
                        {
                            lock (_lock)
                            {
                                _lastError = _lastError.Length > 0 ? _lastError : "Node execution failed.";
                            }
                        }
                        AppendDebug(ok ? "completed: ok (node_update)" : $"completed: failed (node_update) err='{_lastError}'");
                    }
                    else
                    {
                        ok = ((Task<bool>)completedTask).Result;
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
                }
                finally
                {
                    lock (_lock)
                    {
                        if (ReferenceEquals(_activeSession, session))
                            _activeSession = null;
                    }
                    session?.Dispose();
                }
            }
            catch (OperationCanceledException)
            {
                // If we cancelled due to RestartOnChange, don't surface an error.
                if (_cancelRequestedByRestart)
                {
                    AppendDebug("cancelled (restart)");
                }
                else
                {
                    lock (_lock)
                    {
                        _lastError = "Execution cancelled.";
                    }
                    AppendDebug("cancelled");
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
                SetIsRunning(false);
                SetError(_lastError);
                InvalidateOutputs();
                AppendDebug("done");

                // If inputs changed during execution and AutoRun is enabled, run once more.
                if (_autoRunEnabled && _rerunRequested)
                {
                    _rerunRequested = false;
                    AppendDebug("autorun: rerun requested");
                    _ = ExecuteNodeAsync();
                }
            }
        }

        private static bool IsTerminalNodeStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            var s = status.Trim().ToLowerInvariant();
            return s is "completed" or "failed" or "cancelled" or "canceled" or "error";
        }

        private void SetIsRunning(bool isRunning)
        {
            if (_outputPins.TryGetValue("IsRunning", out var pin))
                pin.Value = isRunning;
        }

        private void SetError(string error)
        {
            if (_outputPins.TryGetValue("Error", out var pin))
                pin.Value = error ?? "";
        }

        private void InvalidateOutputs()
        {
            // Throttle invalidation to avoid spamming for streaming outputs.
            var now = DateTime.UtcNow.Ticks;
            var last = Interlocked.Read(ref _lastInvalidateTicks);
            if (now - last < TimeSpan.FromMilliseconds(30).Ticks)
                return;

            Interlocked.Exchange(ref _lastInvalidateTicks, now);
            try
            {
                // This is a best-effort hint to VL that outputs changed.
                // SimpleNodeDescription already exposes an Invalidated observable.
                if (_nodeDescription is SimpleNodeDescription sd)
                    sd.Invalidate();
            }
            catch
            {
                // ignore
            }
        }

        private async Task CancelActiveRunAsync()
        {
            IExecutionSession? session;
            CancellationTokenSource? cts;
            lock (_lock)
            {
                session = _activeSession;
                cts = _manualCancelCts;
            }

            try
            {
                cts?.Cancel();
            }
            catch
            {
                // ignore
            }

            if (session != null)
            {
                try
                {
                    await session.CancelAsync();
                }
                catch
                {
                    // ignore
                }
            }
        }

        private string ComputeInputSignature()
        {
            // Cheap stable signature (no JSON serialization) to detect input changes.
            // Excludes Execute/AutoRun/RestartOnChange pins.
            var sb = new StringBuilder();

            foreach (var kvp in _inputPins.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                if (kvp.Key is "Execute" or "AutoRun" or "RestartOnChange")
                    continue;

                sb.Append(kvp.Key);
                sb.Append('=');
                sb.Append(ValueToSignatureFragment(kvp.Value.Value));
                sb.Append(';');
            }

            return sb.ToString();
        }

        private static string ValueToSignatureFragment(object? value)
        {
            if (value == null)
                return "null";

            switch (value)
            {
                case string s:
                    return $"str:{s}";
                case bool b:
                    return b ? "bool:true" : "bool:false";
                case int i:
                    return $"int:{i}";
                case long l:
                    return $"long:{l}";
                case float f:
                    return $"float:{f.ToString(CultureInfo.InvariantCulture)}";
                case double d:
                    return $"double:{d.ToString(CultureInfo.InvariantCulture)}";
                case decimal m:
                    return $"decimal:{m.ToString(CultureInfo.InvariantCulture)}";
                case byte[] bytes:
                    unchecked
                    {
                        int hash = 17;
                        for (int i = 0; i < Math.Min(bytes.Length, 64); i++)
                            hash = (hash * 31) + bytes[i];
                        return $"bytes:{bytes.Length}:{hash}";
                    }
            }

            if (value is System.Collections.IEnumerable enumerable && value is not string)
            {
                var items = new List<string>();
                int count = 0;
                foreach (var item in enumerable)
                {
                    items.Add(ValueToSignatureFragment(item));
                    count++;
                    if (count >= 10)
                        break;
                }
                return $"seq:{value.GetType().FullName}:{string.Join(",", items)}";
            }

            return $"{value.GetType().FullName}:{value}";
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
                    if (TryRenderTypedText(value, out var rendered))
                        return rendered;

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
                    {
                        if (TryConcatChunkList(value, out var text))
                            return text;

                        // Common: ["hello"] (list of primitive strings) -> unwrap for ergonomics.
                        var list = value.AsListOrEmpty();
                        if (list.Count > 0)
                        {
                            var allStrings = new List<string>(list.Count);
                            var ok = true;
                            foreach (var item in list)
                            {
                                if (item.Kind == NodeToolValueKind.String && item.AsString() is string s)
                                {
                                    allStrings.Add(s);
                                    continue;
                                }

                                if (item.Kind == NodeToolValueKind.Map)
                                {
                                    var m = item.AsMapOrEmpty();
                                    if (m.TryGetValue("type", out var t) &&
                                        string.Equals(t.AsString(), "string", StringComparison.OrdinalIgnoreCase) &&
                                        m.TryGetValue("value", out var inner))
                                    {
                                        allStrings.Add(inner.AsString() ?? inner.ToJsonString());
                                        continue;
                                    }
                                }

                                ok = false;
                                break;
                            }

                            if (ok)
                            {
                                // If it's a list-of-1, treat it as a scalar string (common for workflow outputs).
                                if (allStrings.Count == 1)
                                    return allStrings[0];

                                // If it's a list-of-many, we should NOT concatenate: that loses structure.
                                // In vvvv, list-like results should be represented as a Spread via a string[] pin,
                                // which requires the schema/metadata to expose an array type.
                                // For a string pin, keep structure visible as JSON.
                                return value.ToJsonString();
                            }
                        }

                        // Common: [{ type:"string", value:"..." }] -> unwrap
                        var first = value.AsListOrEmpty().FirstOrDefault(v => v.Kind == NodeToolValueKind.Map);
                        if (first != null && first.Kind == NodeToolValueKind.Map)
                        {
                            var map = first.AsMapOrEmpty();
                            if (map.TryGetValue("type", out var t) &&
                                string.Equals(t.AsString(), "string", StringComparison.OrdinalIgnoreCase) &&
                                map.TryGetValue("value", out var inner))
                            {
                                return inner.AsString() ?? inner.ToJsonString();
                            }
                        }

                        return value.ToJsonString();
                    }

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

        private static bool TryRenderTypedText(NodeToolValue value, out string text)
        {
            text = "";
            if (value.Kind != NodeToolValueKind.Map)
                return false;

            var map = value.AsMapOrEmpty();
            if (!map.TryGetValue("type", out var typeVal))
                return false;

            var typeStr = typeVal.AsString();
            if (string.Equals(typeStr, "string", StringComparison.OrdinalIgnoreCase) &&
                map.TryGetValue("value", out var inner))
            {
                text = inner.AsString() ?? inner.ToJsonString();
                return true;
            }

            if (string.Equals(typeStr, "list", StringComparison.OrdinalIgnoreCase) &&
                map.TryGetValue("value", out var innerList))
            {
                if (TryConcatChunkList(innerList, out var t))
                {
                    text = t;
                    return true;
                }
            }

            if (string.Equals(typeStr, "chunk", StringComparison.OrdinalIgnoreCase) &&
                map.TryGetValue("content", out var content))
            {
                text = content.AsString() ?? "";
                return true;
            }

            return false;
        }

        private static bool TryConcatChunkList(NodeToolValue value, out string text)
        {
            text = "";

            IReadOnlyList<NodeToolValue> list;
            if (value.Kind == NodeToolValueKind.List)
            {
                list = value.AsListOrEmpty();
            }
            else if (value.Kind == NodeToolValueKind.Map)
            {
                var map = value.AsMapOrEmpty();
                if (map.TryGetValue("type", out var t) &&
                    string.Equals(t.AsString(), "list", StringComparison.OrdinalIgnoreCase) &&
                    map.TryGetValue("value", out var v) &&
                    v.Kind == NodeToolValueKind.List)
                {
                    list = v.AsListOrEmpty();
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            var sb = new StringBuilder();
            var sawChunk = false;

            foreach (var item in list)
            {
                if (item.Kind != NodeToolValueKind.Map)
                    continue;

                var typeDisc = item.TypeDiscriminator;
                if (!string.Equals(typeDisc, "chunk", StringComparison.OrdinalIgnoreCase))
                    continue;

                sawChunk = true;
                var map = item.AsMapOrEmpty();
                if (map.TryGetValue("content", out var c) && c.AsString() is string s)
                    sb.Append(s);
            }

            if (!sawChunk)
                return false;

            text = sb.ToString();
            return true;
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
            return VlTypeMapping.MapNodeType(nodeType);
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
                Summary = TextCleanup.StripTrailingPeriodsPerLine(_nodeMetadata.Description ?? _nodeMetadata.Title ?? Name);
                // Keep Remarks consistent with factory tooltips (short; prefer namespace).
                Remarks = TextCleanup.StripTrailingPeriodsPerLine(
                    !string.IsNullOrWhiteSpace(_nodeMetadata.Namespace)
                        ? _nodeMetadata.Namespace.Trim()
                        : (_nodeMetadata.NodeType ?? "").Trim());
                
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
            public void Invalidate() => _invalidated.OnNext(this);

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