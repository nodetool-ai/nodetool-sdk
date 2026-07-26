using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;
using VL.Core;
using VL.Core.CompilerServices;
using VL.Core.Diagnostics;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Execution;
using Nodetool.SDK.Types.Assets;
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
        private volatile bool _isRunning = false;
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

        private volatile bool _cancelRequestedByRestart = false;
        private volatile bool _isDisposed = false;
        private IExecutionSession? _activeSession = null;
        private CancellationTokenSource? _manualCancelCts = null;

        // VL evaluation can be demand-driven; without invalidation pulses, async state changes might not propagate.
        private int _invalidateScheduled;

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
            _inputPins["ExecutionTimeoutSeconds"] = new InternalPin("ExecutionTimeoutSeconds", typeof(int), 0);

            // Add input pins from node properties
            if (_nodeMetadata.Properties != null)
            {
                foreach (var property in _nodeMetadata.Properties)
                {
                    var (vlType, defaultValue) = VlTypeMapping.MapNodeInputType(property.Type);
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
        public uint Identity
        {
            get
            {
                object? path = _nodeContext.Path;
                return (uint)(path?.GetHashCode() ?? RuntimeHelpers.GetHashCode(this));
            }
        }

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
            if (_isDisposed)
                return;

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

                bool shouldRerun;
                lock (_lock)
                {
                    shouldRerun = _autoRunEnabled && _rerunRequested && !_isRunning;
                    if (shouldRerun)
                        _rerunRequested = false;
                }
                if (shouldRerun)
                {
                    AppendDebug("autorun: rerun requested");
                    _ = ExecuteNodeAsync();
                }
            }
            catch (Exception ex)
            {
                _lastError = $"Update error: {VlLog.SafeError(ex)}";
                _isRunning = false;
            }
        }

        /// <summary>
        /// Execute the Nodetool node asynchronously
        /// </summary>
        private async Task ExecuteNodeAsync()
        {
            if (_isDisposed)
                return;

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

            // Ask VL for another evaluation; output pins are only written from Update().
            InvalidateOutputs();

            var timeoutSeconds = NodeToolClientProvider.ResolveExecutionTimeoutSeconds(
                _inputPins.TryGetValue("ExecutionTimeoutSeconds", out var timeoutPin) && timeoutPin.Value is int value ? value : 0);
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
            var localManual = _manualCancelCts;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, localManual?.Token ?? CancellationToken.None);

            try
            {
                AppendDebug($"start node='{_nodeMetadata.NodeType}'");
                // Help debug "changes not taking effect": print the actual loaded DLL + version at runtime.
                var asm = typeof(NodeBase).Assembly;
                VlLog.Debug(
                    $"NodeBase: assembly '{asm.Location}', " +
                    $"version={asm.GetName().Version}");

                if (string.IsNullOrWhiteSpace(_nodeMetadata.NodeType))
                    throw new InvalidOperationException("NodeType is missing from node metadata.");

                // Ensure we have a connected client (user can also do this explicitly via the Connect node)
                if (!NodeToolClientProvider.IsConnected)
                {
                    var connected = await NodeToolClientProvider.ConnectAsync(linked.Token);
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
                            var inputValue = inputPin.Value ?? property.Default ?? "";
                            inputData[property.Name] =
                                await ConvertNodeInputValueAsync(
                                    property.Name,
                                    property.Type,
                                    inputValue,
                                    linked.Token);
                        }
                        else
                        {
                            inputData[property.Name] = property.Default ?? "";
                        }
                    }
                }

                IExecutionSession? session = null;
                // For single-node execution, node_update is often the most reliable completion signal.
                // Some servers may omit job_update in certain edge cases; this prevents IsRunning from hanging.
                var nodeTerminalTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                try
                {
                    var executionOptions = await NodeToolClientProvider
                        .ResolveExecutionOptionsAsync(linked.Token)
                        .ConfigureAwait(false);
                    session = await client.ExecuteNodeAsync(
                        _nodeMetadata.NodeType,
                        inputData,
                        executionOptions,
                        linked.Token);
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
                        ok = await ((Task<bool>)completedTask);
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

                    // Subscribers are attached immediately after ExecuteNodeAsync returns, but a
                    // very fast run can still publish before that point. Reconcile from the
                    // session's buffered snapshot so those values are never lost.
                    ApplyBufferedOutputs(session);
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
                        _lastError = timeoutCts.IsCancellationRequested && localManual?.IsCancellationRequested != true
                            ? $"Execution timed out after {timeoutSeconds} seconds."
                            : "Execution cancelled.";
                    }
                    AppendDebug(timeoutCts.IsCancellationRequested && localManual?.IsCancellationRequested != true
                        ? $"timed out after {timeoutSeconds}s"
                        : "cancelled");
                }
            }
            catch (Exception ex)
            {
                lock (_lock)
                {
                    _lastError = $"Execution error: {VlLog.SafeError(ex)}";
                    _lastOutputs.Clear();
                }
                AppendDebug($"exception: {VlLog.SafeError(ex)}");
            }
            finally
            {
                lock (_lock)
                {
                    _isRunning = false;
                }
                FireOnUpdatePulse();
                InvalidateOutputs();
                AppendDebug("done");
            }
        }

        private static bool IsTerminalNodeStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            var s = status.Trim().ToLowerInvariant();
            return s is "completed" or "failed" or "cancelled" or "canceled" or "error";
        }

        private static string ResolveMediaType(NodeTypeDefinition nodeType)
        {
            var candidates = new[] { nodeType.TypeName, nodeType.Type };
            foreach (var candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;
                var qualifiedToken = candidate
                    .Trim()
                    .Split(new[] { '.', '+', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
                    .LastOrDefault()?
                    .ToLowerInvariant() ?? "";
                var token = new string(qualifiedToken
                    .Where(char.IsLetterOrDigit)
                    .ToArray());
                token = token switch
                {
                    "audioref" => "audio",
                    "videoref" => "video",
                    "documentref" => "document",
                    "assetref" => "asset",
                    "folderref" => "folder",
                    "modelref" => "model_ref",
                    "model3d" or "model3dref" => "model_3d",
                    "fontref" => "font",
                    _ => token
                };
                if (token is
                    "audio" or "video" or "document" or "asset" or "folder" or
                    "model_ref" or "model_3d" or "font")
                {
                    return token;
                }
            }

            return "asset";
        }

        private static async Task<object> ConvertNodeInputValueAsync(
            string inputName,
            NodeTypeDefinition? nodeType,
            object? rawValue,
            CancellationToken cancellationToken)
        {
            if (nodeType == null)
                return rawValue == null ? "" : VlValueConversion.NormalizeForTransport(rawValue);

            var type = nodeType.Type?.Trim().ToLowerInvariant();
            if (type is "list" or "array" or "tuple" &&
                nodeType.TypeArgs?.FirstOrDefault() is { } elementType &&
                rawValue is IEnumerable values and not string)
            {
                var converted = new List<object?>();
                var index = 0;
                foreach (var value in values)
                {
                    converted.Add(await ConvertNodeInputValueAsync(
                        $"{inputName}[{index++}]",
                        elementType,
                        value,
                        cancellationToken));
                }

                return converted.ToArray();
            }

            if (VlTypeMapping.IsFileBackedAssetReference(nodeType))
            {
                return await VlMediaInputAdapter.PrepareAsync(
                    inputName,
                    ResolveMediaType(nodeType),
                    rawValue,
                    cancellationToken);
            }

            return rawValue == null ? "" : VlValueConversion.NormalizeForTransport(rawValue);
        }

        private void ApplyBufferedOutputs(IExecutionSession session)
        {
            var buffered = session.GetLatestOutputs();
            if (_nodeMetadata.Outputs == null || buffered.Count == 0)
                return;

            lock (_lock)
            {
                foreach (var output in _nodeMetadata.Outputs)
                {
                    if (buffered.TryGetValue($"job_result:{output.Name}", out var terminalValue))
                    {
                        _lastOutputs[output.Name] =
                            VlValueConversion.UnwrapTerminalResultEnvelope(terminalValue);
                        continue;
                    }

                    var suffix = $":{output.Name}";
                    var streamedValue = buffered.FirstOrDefault(kvp =>
                        kvp.Key.EndsWith(suffix, StringComparison.Ordinal));
                    if (!string.IsNullOrEmpty(streamedValue.Key))
                        _lastOutputs[output.Name] = streamedValue.Value;
                }
            }
            InvalidateOutputs();
        }

        private void InvalidateOutputs()
        {
            // Coalesce a burst while retaining a trailing invalidation.
            if (Interlocked.CompareExchange(ref _invalidateScheduled, 1, 0) != 0)
                return;

            _ = InvalidateOutputsAfterDelayAsync();
        }

        private async Task InvalidateOutputsAfterDelayAsync()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(16));
            Interlocked.Exchange(ref _invalidateScheduled, 0);
            if (_isDisposed)
                return;
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

            // Invalidation schedules Update(), which performs the actual pin write on VL's thread.
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
            // Excludes execution-control pins.
            var sb = new StringBuilder();

            foreach (var kvp in _inputPins.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                if (kvp.Key is "Execute" or "Cancel" or "AutoRun" or "RestartOnChange" or "ExecutionTimeoutSeconds")
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
                                valueToSet = ConvertNodeToolValueToExpectedType(value, expectedType ?? typeof(string));
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
                    _lastError = $"Output update error: {VlLog.SafeError(ex)}";
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
            if (VlValueConversion.IsSpreadType(pinType))
                return VlValueConversion.CreateEmptySpread(pinType.GetGenericArguments()[0]);
            if (pinType.IsArray && pinType.GetElementType() is Type elementType)
                return Array.CreateInstance(elementType, 0);
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
        internal static object? ConvertNodeToolValueToExpectedType(NodeToolValue value, Type expectedType)
        {
            // Fast paths for common VL pin types
            try
            {
                if (expectedType == typeof(string))
                {
                    return NodeToolValuePresentation.ToDisplayString(value);
                }
                else if (expectedType == typeof(int))
                {
                    if (value.TryGetLong(out var l))
                        return checked((int)l);
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
                else if (expectedType == typeof(byte[]))
                {
                    if (value.TryGetBytes(out var bytes))
                        return bytes;
                    if (value.Kind == NodeToolValueKind.List)
                    {
                        var items = value.AsListOrEmpty();
                        var result = new byte[items.Count];
                        for (var i = 0; i < items.Count; i++)
                        {
                            if (!items[i].TryGetLong(out var item) ||
                                item is < byte.MinValue or > byte.MaxValue)
                            {
                                return Array.Empty<byte>();
                            }
                            result[i] = (byte)item;
                        }
                        return result;
                    }
                    return Array.Empty<byte>();
                }
                else if (VlValueConversion.TryGetCollectionElementType(
                             expectedType,
                             out var elementType))
                {
                    IReadOnlyList<NodeToolValue> values = value.Kind == NodeToolValueKind.List
                        ? value.AsListOrEmpty()
                        : [value];
                    var convertedItems = values
                        .Select(item => ConvertNodeToolValueToExpectedType(item, elementType))
                        .ToArray();
                    return VlValueConversion.CreateCollection(
                        expectedType,
                        elementType,
                        convertedItems);
                }
                else if (typeof(AssetRef).IsAssignableFrom(expectedType))
                {
                    return VlValueConversion.ConvertNodeToolValueToAssetRef(
                        value,
                        expectedType);
                }
                else if (expectedType == typeof(object))
                {
                    return NodeToolValuePresentation.ToDisplayObject(value);
                }
                else
                {
                    var fallback = GetDefaultValueForPinType(expectedType);
                    return VlValueConversion.ConvertOrFallback(
                        value.Raw ?? value.AsString() ?? value.ToJsonString(),
                        expectedType,
                        fallback);
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
            // If VL recreates nodes during patch edits, clearing outputs here will look like a "reset"
            // even though the user didn't change inputs. Keep the instance state intact on dispose.
            _isDisposed = true;
            _ = CancelActiveRunAsync();
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
                pins.Add(new SimplePinDescription("Execute", typeof(bool), false));
                pins.Add(new SimplePinDescription(
                    "Cancel",
                    typeof(bool),
                    false,
                    ExecutionPinVisibility.IsInputVisible("Cancel")));
                pins.Add(new SimplePinDescription(
                    "AutoRun",
                    typeof(bool),
                    false,
                    ExecutionPinVisibility.IsInputVisible("AutoRun")));
                pins.Add(new SimplePinDescription(
                    "RestartOnChange",
                    typeof(bool),
                    false,
                    ExecutionPinVisibility.IsInputVisible("RestartOnChange")));
                pins.Add(new SimplePinDescription(
                    "ExecutionTimeoutSeconds",
                    typeof(int),
                    0,
                    ExecutionPinVisibility.IsInputVisible(
                        "ExecutionTimeoutSeconds")));
                
                if (_nodeMetadata.Properties != null)
                {
                    foreach (var property in _nodeMetadata.Properties)
                    {
                        var (vlType, defaultValue) =
                            VlTypeMapping.MapNodeInputType(property.Type);
                        var targetType = vlType ?? typeof(string);
                        var initial = VlValueConversion.ConvertOrFallback(
                            property.Default,
                            targetType,
                            defaultValue);
                        pins.Add(new SimplePinDescription(
                            property.Name,
                            targetType,
                            initial));
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
                        var (vlType, defaultValue) = MapNodeType(output.Type);
                        pins.Add(new SimplePinDescription(
                            output.Name,
                            vlType ?? typeof(string),
                            defaultValue));
                    }
                }
                
                pins.Add(new SimplePinDescription("IsRunning", typeof(bool), false));
                pins.Add(new SimplePinDescription("On Update", typeof(bool), false));
                pins.Add(new SimplePinDescription(
                    "Error",
                    typeof(string),
                    "",
                    ExecutionPinVisibility.IsOutputVisible("Error")));
                pins.Add(new SimplePinDescription(
                    "Debug",
                    typeof(string),
                    "",
                    ExecutionPinVisibility.IsOutputVisible("Debug")));
                
                return pins.AsReadOnly();
            }
        }

        /// <summary>
        /// Minimal pin description for VL's requirements
        /// </summary>
        private class SimplePinDescription
            : IVLPinDescription,
              IVLPinDescriptionWithVisibility
        {
            public SimplePinDescription(
                string name,
                Type type,
                object? defaultValue,
                bool isVisible = true)
            {
                Name = name;
                Type = type;
                DefaultValue = defaultValue;
                IsVisible = isVisible;
            }

            public string Name { get; }
            public Type Type { get; }
            public object? DefaultValue { get; }
            public string Summary => "";
            public string Remarks => "";
            public IReadOnlyList<string> Tags => new List<string>().AsReadOnly();
            public bool IsVisible { get; }
        }
    }
}
