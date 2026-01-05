using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using SkiaSharp;
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
        private readonly Dictionary<string, SKImage> _latestImages = new(StringComparer.Ordinal);
        private readonly Queue<string> _debugLines = new();
        private const int DebugMaxLines = 30;
        
        // Execution state
        private bool _isRunning = false;
        private string _lastError = "";
        private readonly Dictionary<string, NodeToolValue> _lastOutputs = new(StringComparer.Ordinal);
        private bool _lastExecuteState = false;
        private bool _lastCancelState = false;
        private volatile bool _onUpdatePulse = false;
        private bool _onUpdateHoldArmed = false;
        private bool _hasInitialized = false;
        private bool _prevAutoRunEnabled = false;

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
            _inputPins["Cancel"] = new InternalPin("Cancel", typeof(bool), false);
            _inputPins["AutoRun"] = new InternalPin("AutoRun", typeof(bool), false);
            _inputPins["RestartOnChange"] = new InternalPin("RestartOnChange", typeof(bool), false);

            // Add input pins from node properties
            if (_nodeMetadata.Properties != null)
            {
                foreach (var property in _nodeMetadata.Properties)
                {
                    var (vlType, defaultValue) = MapNodeType(property.Type);
                    var targetType = vlType ?? typeof(string);
                    var initial = VlValueConversion.ConvertOrFallback(property.Default, targetType, defaultValue);
                    var pin = new InternalPin(property.Name, targetType, initial);
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
            _outputPins["On Update"] = new InternalPin("On Update", typeof(bool), false);
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
            // For now, return this without modification.
            // If VL uses immutability here, we may need to return a cloned instance that preserves state.
            return this;
        }

        /// <summary>
        /// Update the node - called by VL on each frame
        /// </summary>
        public void Update()
        {
            try
            {
                // "On Update" is a VL-style pulse that must be observable in the frame loop.
                // We implement it as a 1-frame hold:
                // - when fired, it stays true for the *next* Update() evaluation
                // - then it resets at the beginning of the subsequent Update()
                lock (_lock)
                {
                    if (_onUpdatePulse)
                    {
                        if (_onUpdateHoldArmed)
                        {
                            _onUpdatePulse = false;
                            _onUpdateHoldArmed = false;
                        }
                        else
                        {
                            _onUpdateHoldArmed = true;
                        }
                    }
                }

                // Read current control pin states first.
                var currentExecuteState = _inputPins.TryGetValue("Execute", out var executePin) && executePin.Value is bool bExec && bExec;
                var currentCancelState = _inputPins.TryGetValue("Cancel", out var cancelPin) && cancelPin.Value is bool bCancel && bCancel;
                _autoRunEnabled = _inputPins.TryGetValue("AutoRun", out var autoRunPin) && autoRunPin.Value is bool bAuto && bAuto;
                _restartOnChangeEnabled = _inputPins.TryGetValue("RestartOnChange", out var restartPin) && restartPin.Value is bool bRestart && bRestart;

                // IMPORTANT: first evaluation after load/save/rewire should not trigger execution.
                // VL can replay stored pin values (e.g. Execute=true) on a fresh instance, which would look like a rising edge.
                if (!_hasInitialized)
                {
                    _lastExecuteState = currentExecuteState;
                    _lastCancelState = currentCancelState;
                    _lastInputSignature = ComputeInputSignature();
                    _prevAutoRunEnabled = _autoRunEnabled;
                    _hasInitialized = true;
                    UpdateOutputs();
                    return;
                }

                // Check for Cancel trigger on rising edge
                if (currentCancelState && !_lastCancelState)
                {
                    AppendDebug("cancel requested");
                    _ = CancelActiveRunAsync();
                }
                _lastCancelState = currentCancelState;

                // Check for Execute trigger on rising edge
                if (currentExecuteState && !_lastExecuteState && !_isRunning)
                {
                    // Rising edge detected - execute the node
                    // Keep auto-run signature in sync to avoid immediate duplicate run.
                    _lastInputSignature = ComputeInputSignature();
                    _ = ExecuteNodeAsync();
                }
                _lastExecuteState = currentExecuteState;

                // When AutoRun is turned on, just "arm" it (capture current signature) instead of running immediately.
                if (_autoRunEnabled && !_prevAutoRunEnabled)
                {
                    _lastInputSignature = ComputeInputSignature();
                    _prevAutoRunEnabled = true;
                    UpdateOutputs();
                    return;
                }
                _prevAutoRunEnabled = _autoRunEnabled;

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

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(300));
                var localManual = _manualCancelCts;
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, localManual?.Token ?? CancellationToken.None);

                // Collect input values
                var inputData = new Dictionary<string, object>(StringComparer.Ordinal);
                
                if (_nodeMetadata.Properties != null)
                {
                    foreach (var property in _nodeMetadata.Properties)
                    {
                        if (_inputPins.TryGetValue(property.Name, out var inputPin))
                        {
                            var raw = inputPin.Value ?? property.Default ?? "";
                            inputData[property.Name] = await ConvertInputValueForNodeAsync(property, raw, linked.Token);
                        }
                        else
                        {
                            inputData[property.Name] = await ConvertInputValueForNodeAsync(property, property.Default, linked.Token);
                        }
                    }
                }

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
                FireOnUpdatePulse();
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

        private void FireOnUpdatePulse()
        {
            lock (_lock)
            {
                _onUpdatePulse = true;
                _onUpdateHoldArmed = false;
            }

            // Best-effort immediate set + invalidate so VL notices without waiting for the next frame.
            if (_outputPins.TryGetValue("On Update", out var pin))
                pin.Value = true;
            InvalidateOutputs();
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
            // Excludes Execute/Cancel/AutoRun/RestartOnChange pins.
            var sb = new StringBuilder();

            foreach (var kvp in _inputPins.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                if (kvp.Key is "Execute" or "Cancel" or "AutoRun" or "RestartOnChange")
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
                case SKImage img:
                    return $"skimage:{img.Width}x{img.Height}";
                case DateTime dt:
                    return $"datetime:{dt.ToUniversalTime():O}";
                case Enum e:
                    return $"enum:{e.GetType().FullName}:{Convert.ToInt32(e, CultureInfo.InvariantCulture)}";
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
                if (_outputPins.TryGetValue("On Update", out var onUpdatePin))
                    onUpdatePin.Value = _onUpdatePulse;
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
                                var targetType = expectedType ?? typeof(string);

                                // Special handling for image outputs: decode once per update and keep the last SKImage.
                                if (targetType == typeof(SKImage))
                                {
                                    if (TryExtractImageBytes(value, out var bytes) && bytes.Length > 0)
                                    {
                                        var img = SKImage.FromEncodedData(bytes);
                                        if (img != null)
                                        {
                                            if (_latestImages.TryGetValue(output.Name, out var prev))
                                            {
                                                try { prev.Dispose(); } catch { /* ignore */ }
                                            }
                                            _latestImages[output.Name] = img;
                                            outputPin.Value = img;
                                        }
                                    }
                                    // Keep last value if decode failed.
                                    continue;
                                }

                                valueToSet = ConvertNodeToolValueToExpectedType(value, targetType);
                                outputPin.Value = valueToSet;
                            }
                            else
                            {
                                // IMPORTANT: don't overwrite the pin with default when there's no new output.
                                // VL patches expect "latched" outputs: keep the last value until a new one arrives.
                                // This also prevents the "one frame goes to 0" flicker at the start of execution.
                                //
                                // If this is the first run and nothing has ever been produced, the pin already
                                // contains its initial default set during node creation.
                                continue;
                            }
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
            if (pinType == typeof(SKImage)) return null;
            if (pinType == typeof(DateTime)) return default(DateTime);
            
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

        private async Task<object> ConvertInputValueForNodeAsync(NodeProperty property, object? rawValue, CancellationToken cancellationToken)
        {
            var t = property.Type?.Type?.Trim().ToLowerInvariant() ?? "any";
            var optional = property.Type?.Optional ?? false;

            if (rawValue == null)
                return optional ? null! : "";

            if (t == "datetime")
            {
                if (rawValue is DateTime dt)
                    return ToNodeToolDateTime(dt);

                if (rawValue is DateTimeOffset dto)
                    return ToNodeToolDateTime(dto.UtcDateTime);

                if (rawValue is string s &&
                    DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
                {
                    return ToNodeToolDateTime(DateTime.SpecifyKind(parsed.ToUniversalTime(), DateTimeKind.Utc));
                }

                return rawValue.ToString() ?? "";
            }

            if (t == "enum")
            {
                if (rawValue is Enum e)
                    return StaticEnumRegistry.ToNodeToolLiteral(e);
                return rawValue;
            }

            if (t == "image")
            {
                // Accept SKImage directly: encode to PNG.
                if (rawValue is SKImage skImage)
                {
                    using var data = skImage.Encode(SKEncodedImageFormat.Png, 100);
                    var bytes = data?.ToArray() ?? Array.Empty<byte>();
                    return new Dictionary<string, object?>
                    {
                        ["type"] = "image",
                        ["asset_id"] = null,
                        ["uri"] = "",
                        ["data"] = bytes
                    };
                }

                if (rawValue is byte[] bytesDirect)
                {
                    return new Dictionary<string, object?>
                    {
                        ["type"] = "image",
                        ["asset_id"] = null,
                        ["uri"] = "",
                        ["data"] = bytesDirect
                    };
                }

                // String/URI path: send bytes if local file exists, else send as uri.
                var s = rawValue switch
                {
                    string str => str,
                    Uri u => u.ToString(),
                    _ => rawValue.ToString() ?? ""
                };

                s = s.Trim();
                if (!string.IsNullOrWhiteSpace(s))
                {
                    var fullPath = s;
                    try { fullPath = Path.GetFullPath(s); } catch { /* ignore */ }

                    if (!string.IsNullOrWhiteSpace(fullPath) && File.Exists(fullPath))
                    {
                        var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
                        return new Dictionary<string, object?>
                        {
                            ["type"] = "image",
                            ["asset_id"] = null,
                            ["uri"] = new Uri(fullPath).AbsoluteUri,
                            ["data"] = bytes
                        };
                    }

                    if (Uri.TryCreate(s, UriKind.Absolute, out var uri))
                    {
                        return new Dictionary<string, object?>
                        {
                            ["type"] = "image",
                            ["asset_id"] = null,
                            ["uri"] = uri.ToString(),
                            ["data"] = null
                        };
                    }

                    return new Dictionary<string, object?>
                    {
                        ["type"] = "image",
                        ["asset_id"] = null,
                        ["uri"] = s,
                        ["data"] = null
                    };
                }

                return "";
            }

            return rawValue;
        }

        private static IReadOnlyDictionary<string, NodeToolValue>? ExtractFirstMap(NodeToolValue value)
        {
            if (value.Kind == NodeToolValueKind.Map)
                return value.AsMapOrEmpty();

            if (value.Kind == NodeToolValueKind.List)
            {
                var firstMap = value.AsListOrEmpty().FirstOrDefault(v => v.Kind == NodeToolValueKind.Map);
                if (firstMap != null && firstMap.Kind == NodeToolValueKind.Map)
                    return firstMap.AsMapOrEmpty();
            }

            return null;
        }

        private static bool TryExtractImageBytes(NodeToolValue value, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();

            var map = ExtractFirstMap(value);
            if (map == null)
                return false;

            if (map.TryGetValue("type", out var typeVal) && typeVal.AsString() is string typeStr &&
                !string.Equals(typeStr, "image", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!map.TryGetValue("data", out var dataVal))
                return false;

            if (dataVal.TryGetBytes(out var direct))
            {
                bytes = direct;
                return true;
            }

            if (dataVal.Kind == NodeToolValueKind.List)
            {
                var list = dataVal.AsListOrEmpty();
                var tmp = new byte[list.Count];
                for (var i = 0; i < list.Count; i++)
                {
                    if (!list[i].TryGetLong(out var l))
                        return false;
                    tmp[i] = (byte)l;
                }

                bytes = tmp;
                return true;
            }

            return false;
        }

        private static bool TryExtractDateTimeUtc(NodeToolValue value, out DateTime dtUtc)
        {
            dtUtc = default(DateTime);

            var map = ExtractFirstMap(value);
            if (map != null)
            {
                if (map.TryGetValue("type", out var typeVal) &&
                    typeVal.AsString() is string typeStr &&
                    !string.Equals(typeStr, "datetime", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (TryGetInt(map, "year", out var year) &&
                    TryGetInt(map, "month", out var month) &&
                    TryGetInt(map, "day", out var day))
                {
                    TryGetInt(map, "hour", out var hour);
                    TryGetInt(map, "minute", out var minute);
                    TryGetInt(map, "second", out var second);
                    TryGetInt(map, "microsecond", out var microsecond);

                    var ticks = microsecond > 0 ? microsecond * 10L : 0L;
                    dtUtc = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc).AddTicks(ticks);
                    return true;
                }

                if (map.TryGetValue("value", out var inner) && inner.AsString() is string iso &&
                    DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
                {
                    dtUtc = DateTime.SpecifyKind(parsed.ToUniversalTime(), DateTimeKind.Utc);
                    return true;
                }
            }

            if (value.AsString() is string s &&
                DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            {
                dtUtc = DateTime.SpecifyKind(dt.ToUniversalTime(), DateTimeKind.Utc);
                return true;
            }

            return false;
        }

        private static bool TryGetInt(IReadOnlyDictionary<string, NodeToolValue> map, string key, out int i)
        {
            i = 0;
            if (!map.TryGetValue(key, out var v))
                return false;

            if (v.TryGetLong(out var l))
            {
                i = (int)l;
                return true;
            }

            if (v.TryGetDouble(out var d))
            {
                i = (int)d;
                return true;
            }

            return false;
        }

        private static Dictionary<string, object?> ToNodeToolDateTime(DateTime dt)
        {
            var utc = dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
            var microsecond = (int)((utc.Ticks % TimeSpan.TicksPerSecond) / 10);

            return new Dictionary<string, object?>
            {
                ["type"] = "datetime",
                ["year"] = utc.Year,
                ["month"] = utc.Month,
                ["day"] = utc.Day,
                ["hour"] = utc.Hour,
                ["minute"] = utc.Minute,
                ["second"] = utc.Second,
                ["microsecond"] = microsecond,
                ["tzinfo"] = "UTC",
                ["utc_offset"] = 0
            };
        }

        private static bool TryExtractEnumLiteral(NodeToolValue value, out object? literal)
        {
            literal = null;

            if (value.Kind == NodeToolValueKind.String)
            {
                literal = value.AsString();
                return true;
            }

            if (value.TryGetLong(out var l))
            {
                literal = (int)l;
                return true;
            }

            if (value.Kind == NodeToolValueKind.Map)
            {
                var map = value.AsMapOrEmpty();
                if (map.TryGetValue("value", out var inner))
                {
                    if (inner.Kind == NodeToolValueKind.String)
                    {
                        literal = inner.AsString();
                        return true;
                    }
                    if (inner.TryGetLong(out var innerLong))
                    {
                        literal = (int)innerLong;
                        return true;
                    }
                    literal = inner.Raw ?? inner.ToJsonString();
                    return true;
                }
            }

            return false;
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
                else if (expectedType == typeof(DateTime))
                {
                    return TryExtractDateTimeUtc(value, out var dtUtc) ? dtUtc : default(DateTime);
                }
                else if (expectedType.IsEnum)
                {
                    if (TryExtractEnumLiteral(value, out var literal) &&
                        StaticEnumRegistry.TryFromNodeToolLiteral(expectedType, literal, out var enumValue) &&
                        enumValue != null)
                        return enumValue;

                    if (value.AsString() is string s && Enum.TryParse(expectedType, s, ignoreCase: true, out var parsed))
                        return parsed!;

                    if (value.TryGetLong(out var enumLong))
                        return Enum.ToObject(expectedType, (int)enumLong);

                    return GetDefaultValueForPinType(expectedType);
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
            // If VL recreates nodes during patch edits, clearing outputs here will look like a "reset"
            // even though the user didn't change inputs. Keep the instance state intact on dispose.
            try { _manualCancelCts?.Cancel(); } catch { /* ignore */ }

            foreach (var img in _latestImages.Values)
            {
                try { img.Dispose(); } catch { /* ignore */ }
            }
            _latestImages.Clear();
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