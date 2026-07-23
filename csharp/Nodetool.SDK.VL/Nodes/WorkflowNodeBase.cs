using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Reflection;
using System.Net.Http.Headers;
using SkiaSharp;
using Nodetool.SDK.Api;
using VL.Core;
using Nodetool.SDK.Execution;
using Nodetool.SDK.Values;
using Nodetool.SDK.Types.Assets;
using Nodetool.SDK.VL.Models;
using Nodetool.SDK.VL.Services;
using Nodetool.SDK.VL.Utilities;

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
        private bool _lastCancelState = false;
        private bool _autoRunEnabled = false;
        private bool _restartOnChangeEnabled = false;
        private string _lastInputSignature = "";
        private bool _rerunRequested = false;
        private volatile bool _cancelRequestedByRestart = false;
        private bool _hasInitialized = false;
        private bool _prevAutoRunEnabled = false;

        private volatile IExecutionSession? _activeSession = null;
        private volatile CancellationTokenSource? _manualCancelCts = null;
        private volatile bool _isDisposed = false;
        private volatile bool _isRunning = false;
        private Task _executionTask = Task.CompletedTask;
        private readonly ConcurrentQueue<Action> _pendingStateUpdates = new();
        private readonly Dictionary<string, StringBuilder> _chunkBuffers = new(StringComparer.Ordinal);
        private readonly Dictionary<string, SKImage> _latestImages = new(StringComparer.Ordinal);
        private readonly Dictionary<string, object?> _latchedOutputValues = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, long> _imageLoadVersions = new(StringComparer.Ordinal);
        private readonly CancellationTokenSource _disposeCts = new();
        private readonly Queue<string> _debugLines = new();
        private const int DebugMaxLines = 30;
        private int _invalidateScheduled;
        private static readonly HttpClient ImageHttpClient = new();

        public WorkflowNodeBase(NodeContext nodeContext, WorkflowNodeDescription description, WorkflowDetail workflow)
        {
            _nodeContext = nodeContext ?? throw new ArgumentNullException(nameof(nodeContext));
            _description = description ?? throw new ArgumentNullException(nameof(description));
            _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
            
            // Create input pins
            _inputPins = new Dictionary<string, IVLPin>();
            
            // Add trigger pin
            _inputPins["Trigger"] = new InternalPin("Trigger", typeof(bool), false);
            _inputPins["Cancel"] = new InternalPin("Cancel", typeof(bool), false);
            _inputPins["AutoRun"] = new InternalPin("AutoRun", typeof(bool), false);
            _inputPins["RestartOnChange"] = new InternalPin("RestartOnChange", typeof(bool), false);
            _inputPins["ExecutionTimeoutSeconds"] = new InternalPin("ExecutionTimeoutSeconds", typeof(int), 0);
            
            // Add workflow input pins
            foreach (var property in _workflow.GetInputProperties())
            {
                // Get consistent VL type and default value
                var (vlType, typeDefault) = WorkflowVlTypeMapping.GetTypeAndDefault(property.Type);
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
            _outputPins["Debug"] = new InternalPin("Debug", typeof(string), "");
            _outputPins["InputSchemaJson"] = new InternalPin("InputSchemaJson", typeof(string), "");
            _outputPins["OutputSchemaJson"] = new InternalPin("OutputSchemaJson", typeof(string), "");

            // Set schema pins once (debug convenience)
            try
            {
                _outputPins["InputSchemaJson"].Value = _workflow.InputSchema == null
                    ? ""
                    : JsonSerializer.Serialize(_workflow.InputSchema, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                _outputPins["InputSchemaJson"].Value = "<failed to serialize input schema>";
            }

            try
            {
                _outputPins["OutputSchemaJson"].Value = _workflow.OutputSchema == null
                    ? ""
                    : JsonSerializer.Serialize(_workflow.OutputSchema, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                _outputPins["OutputSchemaJson"].Value = "<failed to serialize output schema>";
            }
            
            // Add workflow output pins
            foreach (var property in _workflow.GetOutputProperties())
            {
                // Get consistent VL type and default value
                var (vlType, defaultValue) = WorkflowVlTypeMapping.GetTypeAndDefault(property.Type);
                _outputPins[property.Name] = new InternalPin(property.Name, vlType, defaultValue);
            }

            foreach (var output in _outputPins)
                _latchedOutputValues[output.Key] = output.Value.Value;

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

            DrainStateUpdates();
            ReapplyLatchedOutputs(_latchedOutputValues, _outputPins);

            try
            {
                // Check for trigger edge (false → true)
                var triggerPin = _inputPins["Trigger"];
                bool currentTriggerState = (bool)(triggerPin.Value ?? false);

                var cancelPin = _inputPins["Cancel"];
                bool currentCancelState = (bool)(cancelPin.Value ?? false);

                _autoRunEnabled = _inputPins.TryGetValue("AutoRun", out var autoRunPin) && autoRunPin.Value is bool bAuto && bAuto;
                _restartOnChangeEnabled = _inputPins.TryGetValue("RestartOnChange", out var restartPin) && restartPin.Value is bool bRestart && bRestart;

                // IMPORTANT: first evaluation after load/save/rewire should not trigger execution.
                if (!_hasInitialized)
                {
                    _lastTriggerState = currentTriggerState;
                    _lastCancelState = currentCancelState;
                    _lastInputSignature = ComputeInputSignature();
                    _prevAutoRunEnabled = _autoRunEnabled;
                    _hasInitialized = true;
                    return;
                }

                // Cancel on rising edge (false → true)
                if (currentCancelState && !_lastCancelState)
                {
                    AppendDebug("cancel requested");
                    _ = CancelActiveRunAsync();
                }
                _lastCancelState = currentCancelState;

                if (currentTriggerState && !_lastTriggerState)
                {
                    // Rising edge detected - execute workflow
                    _lastInputSignature = ComputeInputSignature();
                    StartExecution();
                }

                _lastTriggerState = currentTriggerState;

                // When AutoRun is turned on, just "arm" it (capture current signature) instead of running immediately.
                if (_autoRunEnabled && !_prevAutoRunEnabled)
                {
                    _lastInputSignature = ComputeInputSignature();
                    _prevAutoRunEnabled = true;
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
                            StartExecution();
                        }
                    }
                }

                if (_autoRunEnabled && _rerunRequested && !_isRunning)
                {
                    _rerunRequested = false;
                    AppendDebug("autorun: rerun requested");
                    StartExecution();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WorkflowNodeBase: Error in Update(): {ex.Message}");
                SetError($"Update error: {ex.Message}");
            }
        }

        private void StartExecution()
        {
            if (_isRunning || !_executionTask.IsCompleted || _isDisposed)
                return;
            _executionTask = ExecuteWorkflowAsync();
        }

        private async Task ExecuteWorkflowAsync()
        {
            if (_isRunning) return;
            _isRunning = true;
            CancellationTokenSource? timeoutCts = null;
            var timeoutSeconds = NodeToolClientProvider.ResolveExecutionTimeoutSeconds(
                _inputPins.TryGetValue("ExecutionTimeoutSeconds", out var timeoutPin) && timeoutPin.Value is int value ? value : 0);
            try
            {
                AppendDebug($"start workflow='{_workflow.Name}'");
                Console.WriteLine($"WorkflowNodeBase: Starting execution of workflow '{_workflow.Name}'");
                // Help debug "changes not taking effect": print the actual loaded DLL + version at runtime.
                var asm = typeof(WorkflowNodeBase).Assembly;
                Console.WriteLine($"WorkflowNodeBase: Using assembly '{asm.Location}', version={asm.GetName().Version}");

                // Reset per-run chunk buffers so streaming output doesn't accumulate across runs.
                _chunkBuffers.Clear();
                _debugLines.Clear();
                if (_outputPins.TryGetValue("Debug", out var debugPin))
                    debugPin.Value = "";
                _rerunRequested = false;
                _cancelRequestedByRestart = false;

                _manualCancelCts?.Dispose();
                _manualCancelCts = new CancellationTokenSource();

                SetIsRunning(true);
                SetError("");

                if (_workflow.InputSchema?.Properties != null && _workflow.InputSchema.Properties.Count > 0)
                {
                    var keys = string.Join(", ", _workflow.InputSchema.Properties.Keys);
                    Console.WriteLine($"WorkflowNodeBase: Input schema keys: {keys}");
                }

                timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                var localManual = _manualCancelCts;
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, localManual?.Token ?? CancellationToken.None);

                // Ensure connection
                if (!NodeToolClientProvider.IsConnected)
                {
                    var connected = await NodeToolClientProvider.ConnectAsync(linked.Token);
                    if (!connected)
                    {
                        throw new InvalidOperationException(NodeToolClientProvider.LastError ?? "Failed to connect to NodeTool server.");
                    }
                }
                AppendDebug("connected");

                var client = NodeToolClientProvider.GetClient();

                // Collect inputs from pins (excluding Trigger) and adapt values based on workflow schema.
                var parameters = await BuildWorkflowParametersAsync(linked.Token);
                var outputRoutes = BuildOutputRoutingTable(_workflow);
                // Execute by ID so repeated runs do not pay the HTTP name lookup cost.
                var session = await client.ExecuteWorkflowAsync(_workflow.Id, parameters, linked.Token);
                _activeSession = session;

                session.ProgressChanged += progress =>
                {
                    // Lightweight progress trace (helps diagnose "runs forever")
                    Console.WriteLine($"WorkflowNodeBase: Workflow '{_workflow.Name}' progress: {progress:P0}");
                    AppendDebug($"progress={(progress * 100):0}%");
                };

                session.NodeUpdated += update =>
                {
                    if (!string.IsNullOrWhiteSpace(update.error))
                    {
                        Console.WriteLine($"WorkflowNodeBase: Node error in workflow '{_workflow.Name}': {update.error}");
                        SetError(update.error);
                        AppendDebug($"node_error: {update.error}");
                    }
                };

                session.Completed += (success, err) =>
                {
                    if (!success)
                    {
                        Console.WriteLine($"WorkflowNodeBase: Workflow '{_workflow.Name}' completed with error: {err}");
                        SetError(err ?? "Workflow failed.");
                        AppendDebug($"completed: failed err='{err ?? ""}'");
                    }
                    else
                    {
                        AppendDebug("completed: ok");
                    }
                };

                session.OutputReceived += update =>
                {
                    EnqueueStateUpdate(() => ApplyLiveOutputUpdate(outputRoutes, update));
                };

                // Live output updates are progressive only. A workflow is finished when the
                // terminal job_update arrives; its result is the authoritative final snapshot.
                var ok = await session.WaitForCompletionAsync(linked.Token);

                if (!ok)
                {
                    throw new InvalidOperationException(session.ErrorMessage ?? "Workflow execution failed.");
                }

                // Canonical final outputs are delivered via job_update.result (keys match output_schema, e.g. "inverted").
                // This ensures we don't miss fast output_update events and still populate pins deterministically.
                ApplyFinalOutputsFromSession(session);

                Console.WriteLine($"WorkflowNodeBase: Workflow '{_workflow.Name}' execution completed");
                AppendDebug("done");
                SetIsRunning(false);
            }
            catch (OperationCanceledException)
            {
                if (_cancelRequestedByRestart)
                {
                    AppendDebug("cancelled (restart)");
                    SetIsRunning(false);
                }
                else
                {
                    var timedOut = timeoutCts?.IsCancellationRequested == true && _manualCancelCts?.IsCancellationRequested != true;
                    SetError(timedOut ? $"Execution timed out after {timeoutSeconds} seconds." : "Execution cancelled.");
                    AppendDebug(timedOut ? $"timed out after {timeoutSeconds}s" : "cancelled");
                    SetIsRunning(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WorkflowNodeBase: Error executing workflow '{_workflow.Name}': {ex.Message}");
                SetError($"Execution failed: {ex.Message}");
                AppendDebug($"exception: {ex.Message}");
                SetIsRunning(false);
            }
            finally
            {
                timeoutCts?.Dispose();
                _activeSession = null;
                _isRunning = false;
            }
        }

        private async Task CancelActiveRunAsync()
        {
            var session = _activeSession;
            var cts = _manualCancelCts;

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

        private static IReadOnlyDictionary<string, string> BuildOutputRoutingTable(
            WorkflowDetail workflow)
        {
            var routes = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var output in workflow.Interface?.Outputs ?? [])
            {
                routes[$"node:{output.NodeId}"] = output.Name;
                routes[$"output:{output.Name}"] = output.Name;
                routes[$"name:{output.Name}"] = output.Name;
            }
            return routes;
        }

        private static string? ResolveOutputPinName(
            IReadOnlyDictionary<string, string> routes,
            ExecutionOutputUpdate update)
        {
            // A scoped node ID is authoritative. Do not let Preview or upstream
            // node updates overwrite a workflow output merely because they use
            // the same generic handle name (commonly "output").
            if (!string.IsNullOrWhiteSpace(update.NodeId))
            {
                return routes.TryGetValue($"node:{update.NodeId}", out var byNode)
                    ? byNode
                    : null;
            }

            if (routes.TryGetValue($"output:{update.OutputName}", out var byOutput))
                return byOutput;
            return routes.TryGetValue($"name:{update.NodeName}", out var byName)
                ? byName
                : null;
        }

        private void ApplyLiveOutputUpdate(
            IReadOnlyDictionary<string, string> outputRoutes,
            ExecutionOutputUpdate update)
        {
            var pinName = ResolveOutputPinName(outputRoutes, update);
            var pin = pinName != null && _outputPins.TryGetValue(pinName, out var routedPin)
                ? routedPin
                : null;
            Console.WriteLine(
                $"WorkflowNodeBase: output_update received: node_id='{update.NodeId}' output_name='{update.OutputName}' node_name='{update.NodeName}' output_type='{update.OutputType}' pin='{pinName ?? "<none>"}'");
            AppendDebugCore($"{DateTime.Now:HH:mm:ss.fff} output_update: {update.OutputName} type={update.OutputType} pin={pinName ?? "<none>"}");

            if (pin == null || pinName == null)
                return;

            // IVLPin doesn't expose Type; our InternalPin does.
            var expectedType = (pin as InternalPin)?.Type ?? typeof(string);
            if (TryAccumulateChunk(_chunkBuffers, pinName, update, out var chunkText))
            {
                // Keep accumulated content when done=true carries an empty final chunk.
                SetOutputPinValue(pinName, pin, chunkText);
                return;
            }

            // Generic Output nodes currently advertise output_type="any". The
            // discovered workflow interface is authoritative for the VL pin type.
            if (expectedType == typeof(SKImage))
            {
                ApplyOrScheduleImageOutput(pinName, pin, update.Value);
                return;
            }

            SetOutputPinValue(
                pinName,
                pin,
                ConvertNodeToolValueToExpectedType(update.Value, expectedType));
        }

        internal static bool TryAccumulateChunk(
            IDictionary<string, StringBuilder> buffers,
            string pinName,
            ExecutionOutputUpdate update,
            out string text)
        {
            text = "";
            if (update.Value.Kind != NodeToolValueKind.Map ||
                !string.Equals(update.Value.TypeDiscriminator, "chunk", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!buffers.TryGetValue(pinName, out var buffer))
            {
                buffer = new StringBuilder();
                buffers[pinName] = buffer;
            }

            if (string.Equals(update.Disposition, "replace", StringComparison.OrdinalIgnoreCase))
                buffer.Clear();

            var map = update.Value.AsMapOrEmpty();
            if (map.TryGetValue("content", out var content))
                buffer.Append(content.AsString() ?? "");

            text = buffer.ToString();
            return true;
        }

        private string ComputeInputSignature()
        {
            // Cheap stable signature to detect input changes; excludes Trigger/AutoRun.
            var sb = new StringBuilder();
            foreach (var kvp in _inputPins.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                if (kvp.Key is "Trigger" or "Cancel" or "AutoRun" or "RestartOnChange" or "ExecutionTimeoutSeconds")
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

        private void AppendDebug(string line)
        {
            var ts = DateTime.Now.ToString("HH:mm:ss.fff");
            EnqueueStateUpdate(() => AppendDebugCore($"{ts} {line}"));
        }

        private void AppendDebugCore(string message)
        {
            try
            {
                while (_debugLines.Count >= DebugMaxLines)
                    _debugLines.Dequeue();
                _debugLines.Enqueue(message);

                if (_outputPins.TryGetValue("Debug", out var pin))
                {
                    SetOutputPinValue("Debug", pin, string.Join(Environment.NewLine, _debugLines));
                }
            }
            catch
            {
                // ignore debug failures
            }
        }

        private void EnqueueStateUpdate(Action update)
        {
            if (!_isDisposed)
            {
                _pendingStateUpdates.Enqueue(update);
                RequestVlUpdate();
            }
        }

        private void RequestVlUpdate()
        {
            if (Interlocked.CompareExchange(ref _invalidateScheduled, 1, 0) != 0)
                return;

            _ = InvalidateAfterDelayAsync();
        }

        private async Task InvalidateAfterDelayAsync()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(16));
            Interlocked.Exchange(ref _invalidateScheduled, 0);
            if (_isDisposed)
                return;
            try { _description.Invalidate(); } catch { /* best-effort scheduling hint */ }
        }

        private void DrainStateUpdates()
        {
            // Drain a bounded snapshot so a busy stream cannot monopolize VL's frame.
            var count = _pendingStateUpdates.Count;
            for (var i = 0; i < count && _pendingStateUpdates.TryDequeue(out var update); i++)
            {
                try
                {
                    update();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WorkflowNodeBase: Failed to apply queued state: {ex.Message}");
                    SetErrorCore($"State update failed: {ex.Message}");
                }
            }

            // Count is only a snapshot. Keep evaluating if producers added more work
            // while this frame was draining the queue.
            if (!_pendingStateUpdates.IsEmpty)
                RequestVlUpdate();
        }

        private void ApplyFinalOutputsFromSession(IExecutionSession session)
        {
            var outputs = session.GetLatestOutputs();
            EnqueueStateUpdate(() => ApplyFinalOutputs(outputs));
        }

        private void ApplyFinalOutputs(IReadOnlyDictionary<string, NodeToolValue> outputs)
        {
            try
            {
                foreach (var kvp in outputs)
                {
                    var key = kvp.Key;
                    if (!key.StartsWith("job_result:", StringComparison.Ordinal))
                        continue;

                    var outputName = key.Substring("job_result:".Length);
                    if (_outputPins.TryGetValue(outputName, out var pin))
                    {
                        var expectedType = (pin as InternalPin)?.Type ?? typeof(string);

                        // Final outputs: for images, prefer returning a decoded SKImage.
                        // IMPORTANT: do not overwrite an already-received SKImage with null/default if the final
                        // job_update.result omits a usable byte payload or URI.
                        if (expectedType == typeof(SKImage))
                        {
                            ApplyOrScheduleImageOutput(outputName, pin, kvp.Value);
                            continue;
                        }

                        SetOutputPinValue(
                            outputName,
                            pin,
                            ConvertNodeToolValueToExpectedType(kvp.Value, expectedType));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WorkflowNodeBase: Failed to apply final outputs: {ex.Message}");
            }
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

        internal static bool TryExtractImageBytes(NodeToolValue value, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();

            if (value.TryGetBytes(out var directValue))
            {
                bytes = directValue;
                return bytes.Length > 0;
            }

            var map = ExtractFirstMap(value);
            if (map == null)
                return false;

            if (map.TryGetValue("type", out var typeVal) && typeVal.AsString() is string typeStr &&
                !IsImageTypeDiscriminator(typeStr))
                return false;

            if (map.TryGetValue("data", out var dataVal))
            {
                if (dataVal.TryGetBytes(out var direct))
                {
                    bytes = direct;
                    return bytes.Length > 0;
                }

                if (dataVal.Kind == NodeToolValueKind.List)
                {
                    // data may come through as [137,80,78,71,...]
                    var list = dataVal.AsListOrEmpty();
                    var tmp = new byte[list.Count];
                    for (var i = 0; i < list.Count; i++)
                    {
                        if (!list[i].TryGetLong(out var l) || l is < byte.MinValue or > byte.MaxValue)
                            return false;
                        tmp[i] = (byte)l;
                    }

                    bytes = tmp;
                    return bytes.Length > 0;
                }

                if (dataVal.AsString() is string encoded &&
                    TryDecodeBase64OrDataUri(encoded, out bytes))
                {
                    return bytes.Length > 0;
                }
            }

            return TryExtractImageUri(value, out var uri) &&
                   TryDecodeBase64OrDataUri(uri, out bytes) &&
                   bytes.Length > 0;
        }

        internal static bool TryExtractImageUri(NodeToolValue value, out string uri)
        {
            uri = "";
            var map = ExtractFirstMap(value);
            if (map == null)
                return false;

            if (map.TryGetValue("type", out var typeVal) && typeVal.AsString() is string typeStr &&
                !IsImageTypeDiscriminator(typeStr))
            {
                return false;
            }

            if (map.TryGetValue("uri", out var uriValue))
            {
                uri = uriValue.AsString() ?? "";
                if (!string.IsNullOrWhiteSpace(uri))
                    return true;
            }

            if (map.TryGetValue("asset_id", out var assetIdValue) &&
                assetIdValue.AsString() is { } assetId &&
                !string.IsNullOrWhiteSpace(assetId))
            {
                uri = $"asset:{Uri.EscapeDataString(assetId)}";
                return true;
            }

            return false;
        }

        private static bool IsImageTypeDiscriminator(string type)
            => string.Equals(type, "image", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(type, "ImageRef", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(type, "image_ref", StringComparison.OrdinalIgnoreCase);

        private static bool TryDecodeBase64OrDataUri(string value, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            var encoded = value.Trim();
            if (encoded.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var comma = encoded.IndexOf(',');
                if (comma < 0 ||
                    encoded.AsSpan(0, comma).IndexOf(";base64", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
                encoded = encoded[(comma + 1)..];
            }

            try
            {
                bytes = Convert.FromBase64String(encoded);
                return bytes.Length > 0;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private void ApplyOrScheduleImageOutput(string pinName, IVLPin pin, NodeToolValue value)
        {
            if (TryExtractImageBytes(value, out var bytes) && bytes.Length > 0)
            {
                _imageLoadVersions.AddOrUpdate(pinName, 1, static (_, current) => current + 1);
                ApplyDecodedImage(pinName, pin, bytes);
                return;
            }

            if (!TryExtractImageUri(value, out var uri))
            {
                // Keep a valid progressive value if the terminal snapshot contains
                // only an incomplete reference.
                return;
            }

            var version = _imageLoadVersions.AddOrUpdate(pinName, 1, static (_, current) => current + 1);
            _ = LoadImageOutputAsync(pinName, pin, uri, version, _disposeCts.Token);
        }

        private void ApplyDecodedImage(string pinName, IVLPin pin, byte[] bytes)
        {
            var image = SKImage.FromEncodedData(bytes);
            if (image == null)
            {
                SetErrorCore($"Failed to decode image bytes for output '{pinName}'.");
                return;
            }

            ApplyImageOutput(pinName, pin, image);
        }

        private void ApplyImageOutput(string pinName, IVLPin pin, SKImage image)
        {
            if (_latestImages.TryGetValue(pinName, out var previous))
                previous.Dispose();
            _latestImages[pinName] = image;
            SetOutputPinValue(pinName, pin, image);
            AppendDebugCore($"{DateTime.Now:HH:mm:ss.fff} image ready: {pinName} {image.Width}x{image.Height}");
            Console.WriteLine(
                $"WorkflowNodeBase: image output ready: pin='{pinName}' size={image.Width}x{image.Height}");
        }

        private async Task LoadImageOutputAsync(
            string pinName,
            IVLPin pin,
            string uriText,
            long version,
            CancellationToken cancellationToken)
        {
            try
            {
                var uri = await ResolveImageUriAsync(uriText, cancellationToken).ConfigureAwait(false);
                if (uri == null)
                    throw new InvalidOperationException($"Unsupported image URI '{uriText}'.");

                SKImage? image;
                if (uri.IsFile)
                {
                    await using var stream = new FileStream(
                        uri.LocalPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        bufferSize: 81920,
                        useAsync: true);
                    image = SKImage.FromEncodedData(stream);
                }
                else if (uri.Scheme is "http" or "https")
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    var token = NodeToolClientProvider.CurrentAuthToken;
                    if (!string.IsNullOrWhiteSpace(token) && IsSameOrigin(uri, NodeToolClientProvider.CurrentApiBaseUrl))
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    using var response = await ImageHttpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                    image = SKImage.FromEncodedData(stream);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported image URI scheme '{uri.Scheme}'.");
                }

                if (image == null)
                    throw new InvalidOperationException("Downloaded bytes are not a supported image.");

                EnqueueStateUpdate(() =>
                {
                    if (_isDisposed ||
                        !_imageLoadVersions.TryGetValue(pinName, out var currentVersion) ||
                        currentVersion != version)
                    {
                        image.Dispose();
                        return;
                    }

                    ApplyImageOutput(pinName, pin, image);
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Node disposal cancels outstanding media downloads.
            }
            catch (Exception ex)
            {
                EnqueueStateUpdate(() =>
                {
                    if (_imageLoadVersions.TryGetValue(pinName, out var currentVersion) &&
                        currentVersion == version)
                    {
                        SetErrorCore($"Failed to load image output '{pinName}': {ex.Message}");
                    }
                });
            }
        }

        private static async Task<Uri?> ResolveImageUriAsync(
            string value,
            CancellationToken cancellationToken)
        {
            if (TryExtractAssetKey(value, out var assetKey) &&
                string.IsNullOrEmpty(Path.GetExtension(assetKey)) &&
                NodeToolClientProvider.IsConnected)
            {
                var asset = await NodeToolClientProvider
                    .GetClient()
                    .GetAssetAsync(assetKey, cancellationToken)
                    .ConfigureAwait(false);
                var materializedUri = asset?.GetUrl ?? asset?.Uri;
                if (!string.IsNullOrWhiteSpace(materializedUri) &&
                    !string.Equals(materializedUri, value, StringComparison.OrdinalIgnoreCase))
                {
                    return ResolveImageUri(materializedUri);
                }
            }

            return ResolveImageUri(value);
        }

        internal static Uri? ResolveImageUri(string value)
        {
            if (TryExtractAssetKey(value, out var assetKey))
            {
                var assetApiBase = NodeToolClientProvider.CurrentApiBaseUrl;
                return assetApiBase == null
                    ? null
                    : new Uri(
                        assetApiBase,
                        $"/api/storage/{Uri.EscapeDataString(assetKey)}");
            }

            if (Uri.TryCreate(value, UriKind.Absolute, out var absolute))
                return absolute;

            var apiBase = NodeToolClientProvider.CurrentApiBaseUrl;
            return apiBase == null || !Uri.TryCreate(apiBase, value, out var relative)
                ? null
                : relative;
        }

        internal static bool TryExtractAssetKey(string value, out string assetKey)
        {
            assetKey = "";
            var trimmed = value.Trim();
            if (!trimmed.StartsWith("asset:", StringComparison.OrdinalIgnoreCase))
                return false;

            var encodedId = trimmed["asset:".Length..].TrimStart('/');
            if (string.IsNullOrWhiteSpace(encodedId))
                return false;

            assetKey = Uri.UnescapeDataString(encodedId);
            return !string.IsNullOrWhiteSpace(assetKey);
        }

        private static bool IsSameOrigin(Uri target, Uri? origin)
            => origin != null &&
               string.Equals(target.Scheme, origin.Scheme, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(target.Host, origin.Host, StringComparison.OrdinalIgnoreCase) &&
               target.Port == origin.Port;

        private void SetOutputPinValue(string pinName, IVLPin pin, object? value)
        {
            _latchedOutputValues[pinName] = value;
            pin.Value = value;
        }

        internal static void ReapplyLatchedOutputs(
            IReadOnlyDictionary<string, object?> latchedValues,
            IReadOnlyDictionary<string, IVLPin> outputPins)
        {
            foreach (var output in latchedValues)
            {
                if (outputPins.TryGetValue(output.Key, out var pin))
                    pin.Value = output.Value;
            }
        }

        private void SetIsRunning(bool isRunning)
        {
            EnqueueStateUpdate(() => SetIsRunningCore(isRunning));
        }

        private void SetIsRunningCore(bool isRunning)
        {
            if (_outputPins.TryGetValue("IsRunning", out var pin))
            {
                SetOutputPinValue("IsRunning", pin, isRunning);
            }
        }

        private void SetError(string error)
        {
            EnqueueStateUpdate(() => SetErrorCore(error));
        }

        private void SetErrorCore(string error)
        {
            if (_outputPins.TryGetValue("Error", out var pin))
            {
                SetOutputPinValue("Error", pin, error);
            }
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
            if (VlValueConversion.IsSpreadType(vlType))
                return VlValueConversion.CreateEmptySpread(vlType.GetGenericArguments()[0]);
            if (vlType.IsArray && vlType.GetElementType() is Type elementType)
                return Array.CreateInstance(elementType, 0);
            if (vlType == typeof(SKImage)) return null!;
            
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
            return VlValueConversion.ConvertOrFallback(value, expectedType, GetDefaultValueForVLType(expectedType))
                   ?? GetDefaultValueForVLType(expectedType);
        }

        internal static object ConvertNodeToolValueToExpectedType(NodeToolValue value, Type expectedType)
        {
            // Workflow runner terminal results are output-name -> value[] even for
            // scalar schema properties. Preserve arrays for spread pins, but unwrap
            // a singleton container before scalar conversion.
            if (!VlValueConversion.TryGetCollectionElementType(expectedType, out _) &&
                value.Kind == NodeToolValueKind.List &&
                value.AsListOrEmpty() is { Count: 1 } singleton)
            {
                return ConvertNodeToolValueToExpectedType(singleton[0], expectedType);
            }

            // Prefer primitives when possible; fall back to JSON string for complex values.
            if (expectedType == typeof(string))
            {
                if (TryRenderTypedText(value, out var rendered))
                    return rendered;

                // Avoid ToString() on dictionaries/lists (it yields "System.Collections.Generic.Dictionary`2[...]").
                // Prefer common fields first; otherwise fall back to JSON.
                if (value.Kind == NodeToolValueKind.Map)
                {
                    var map = value.AsMapOrEmpty();
                    if (map.TryGetValue("uri", out var uriVal))
                        return uriVal.AsString() ?? uriVal.ToJsonString();
                    if (map.TryGetValue("asset_id", out var assetIdVal))
                        return assetIdVal.AsString() ?? assetIdVal.ToJsonString();

                    // Common chunk/text payload shapes
                    if (map.TryGetValue("text", out var textVal) && textVal.AsString() is string textStr)
                        return textStr;

                    if (map.TryGetValue("delta", out var deltaVal))
                        return deltaVal.AsString() ?? deltaVal.ToJsonString();

                    if (map.TryGetValue("chunk", out var chunkVal))
                        return chunkVal.AsString() ?? chunkVal.ToJsonString();

                    if (map.TryGetValue("value", out var valueVal))
                        return valueVal.AsString() ?? valueVal.ToJsonString();

                    if (map.TryGetValue("result", out var resultVal))
                        return resultVal.AsString() ?? resultVal.ToJsonString();

                    return value.ToJsonString();
                }

                if (value.Kind == NodeToolValueKind.List)
                {
                    // Common: list of chunk objects -> concatenate content
                    if (TryConcatChunkList(value, out var text))
                        return text;

                    // Common: ["hello"] (list of primitive strings) -> unwrap for ergonomics.
                    // Output nodes sometimes return a list even when a single string is expected.
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

            if (typeof(AssetRef).IsAssignableFrom(expectedType) &&
                ExtractFirstMap(value) is { } assetMap)
            {
                return ConvertAssetRefValue(assetMap, expectedType);
            }

            if (expectedType == typeof(object))
            {
                if (value.Kind == NodeToolValueKind.Map)
                {
                    var map = value.AsMapOrEmpty();
                    if (map.TryGetValue("text", out var textVal) && textVal.AsString() is string textStr)
                        return textStr;
                    if (map.TryGetValue("delta", out var deltaVal))
                        return deltaVal.AsString() ?? deltaVal.ToJsonString();
                    if (map.TryGetValue("chunk", out var chunkVal))
                        return chunkVal.AsString() ?? chunkVal.ToJsonString();
                    return value.ToJsonString();
                }
                if (value.Kind == NodeToolValueKind.List)
                    return value.ToJsonString();
                return value.Raw ?? "";
            }

            if (expectedType == typeof(int) && value.TryGetLong(out var l))
            {
                return (int)l;
            }

            if (expectedType == typeof(float) && value.TryGetDouble(out var d))
            {
                return (float)d;
            }

            if (expectedType == typeof(bool) && value.TryGetBool(out var b))
            {
                return b;
            }

            // Collection outputs use VL-native Spread<T> for list metadata.
            if (VlValueConversion.TryGetCollectionElementType(expectedType, out var elementType))
            {
                IReadOnlyList<NodeToolValue> values = value.Kind == NodeToolValueKind.List
                    ? value.AsListOrEmpty()
                    : [value];
                var convertedItems = values
                    .Select(item => ConvertNodeToolValueToExpectedType(item, elementType))
                    .Cast<object?>()
                    .ToArray();
                return VlValueConversion.CreateCollection(
                    expectedType,
                    elementType,
                    convertedItems);
            }

            return ConvertToExpectedType(value.Raw ?? value.AsString() ?? value.ToJsonString(), expectedType);
        }

        private static AssetRef ConvertAssetRefValue(
            IReadOnlyDictionary<string, NodeToolValue> map,
            Type expectedType)
        {
            var result = (AssetRef)(Activator.CreateInstance(expectedType)
                ?? throw new InvalidOperationException($"Cannot create asset reference type {expectedType.Name}."));
            if (map.TryGetValue("uri", out var uri))
                result.Uri = uri.AsString() ?? "";
            if (map.TryGetValue("asset_id", out var assetId))
                result.AssetId = assetId.AsString();
            if (map.TryGetValue("data", out var data))
            {
                result.Data = data.TryGetBytes(out var bytes)
                    ? bytes
                    : data.Raw;
            }
            if (result is VideoRef video)
            {
                if (map.TryGetValue("duration", out var duration) && duration.TryGetDouble(out var seconds))
                    video.Duration = (float)seconds;
                if (map.TryGetValue("format", out var format))
                    video.Format = format.AsString();
            }
            return result;
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

                // Plain string list: unwrap single-element or join multiple.
                if (innerList.Kind == NodeToolValueKind.List)
                {
                    var items = innerList.AsListOrEmpty();
                    var strings = new List<string>(items.Count);
                    var allStr = true;
                    foreach (var item in items)
                    {
                        var s = item.AsString();
                        if (s != null)
                        {
                            strings.Add(s);
                        }
                        else
                        {
                            allStr = false;
                            break;
                        }
                    }

                    if (allStr && strings.Count > 0)
                    {
                        text = strings.Count == 1 ? strings[0] : string.Join("\n", strings);
                        return true;
                    }
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
                // typed wrapper {type:"list", value:[...]}
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

                var map = item.AsMapOrEmpty();
                var typeDisc = item.TypeDiscriminator;
                if (!string.Equals(typeDisc, "chunk", StringComparison.OrdinalIgnoreCase))
                    continue;

                sawChunk = true;
                if (map.TryGetValue("content", out var c) && c.AsString() is string s)
                    sb.Append(s);
            }

            if (!sawChunk)
                return false;

            text = sb.ToString();
            return true;
        }

        private async Task<Dictionary<string, object>> BuildWorkflowParametersAsync(CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<string, object>(StringComparer.Ordinal);

            foreach (var kvp in _inputPins)
            {
                if (kvp.Key is "Trigger" or "Cancel" or "AutoRun" or "RestartOnChange" or "ExecutionTimeoutSeconds")
                    continue;

                var raw = kvp.Value.Value;
                parameters[kvp.Key] = await ConvertInputValueForWorkflowAsync(kvp.Key, raw, cancellationToken);
            }

            return parameters;
        }

        private async Task<object> ConvertInputValueForWorkflowAsync(string inputName, object? rawValue, CancellationToken cancellationToken)
        {
            var interfaceType = _workflow.Interface?.Inputs
                .FirstOrDefault(input => string.Equals(input.Name, inputName, StringComparison.Ordinal))
                ?.Type.Type.ToLowerInvariant();
            if (interfaceType is "image" or "audio" or "video" or "document" or "asset" or "asset_ref")
                return await ConvertMediaInputAsync(inputName, interfaceType, rawValue, cancellationToken);

            // If we don't have schema, pass through as string for compatibility with TEST_SDK_01.
            var propDef = _workflow.InputSchema?.Properties != null && _workflow.InputSchema.Properties.TryGetValue(inputName, out var p)
                ? p
                : null;

            // Schema-based detection only (no name heuristics).
            // This must handle $ref / anyOf / oneOf / allOf properly.
            if (IsImageSchema(propDef, _workflow.InputSchema))
            {
                var rawType = rawValue?.GetType().FullName ?? "<null>";

                // Accept bytes directly (future: SKImage/Stride can be converted to bytes by helper nodes)
                if (rawValue is byte[] bytesDirect)
                {
                    Console.WriteLine($"WorkflowNodeBase: Image input '{inputName}' received byte[] (len={bytesDirect.Length})");
                    return new Dictionary<string, object?>
                    {
                        ["type"] = "image",
                        ["asset_id"] = null,
                        ["uri"] = "",
                        ["data"] = bytesDirect
                    };
                }

                // Normalize to string (VL sometimes provides non-string path types)
                var s = rawValue switch
                {
                    null => null,
                    string str => str,
                    Uri u => u.ToString(),
                    _ => rawValue.ToString()
                };

                s = s?.Trim();
                if (!string.IsNullOrEmpty(s) && s.Length >= 2 && s.StartsWith("\"") && s.EndsWith("\""))
                    s = s.Trim('"');

                Console.WriteLine($"WorkflowNodeBase: Image input '{inputName}' rawType={rawType} value='{s ?? "<null>"}'");
                DumpSchemaForDebug(inputName, propDef, _workflow.InputSchema);

                if (string.IsNullOrWhiteSpace(s))
                {
                    throw new InvalidOperationException(
                        $"Image input '{inputName}' is empty. Provide a file path/URL, or bytes.");
                }

                if (!string.IsNullOrWhiteSpace(s))
                {
                    // Local file path → send bytes directly (no extra asset creation step).
                    // Normalize first so relative paths work.
                    var fullPath = s;
                    try { fullPath = Path.GetFullPath(s); } catch { /* ignore */ }

                    if (!string.IsNullOrWhiteSpace(fullPath) && File.Exists(fullPath))
                    {
                        Console.WriteLine($"WorkflowNodeBase: Image input '{inputName}' using local file path: {fullPath}");
                        var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
                        var fileUri = new Uri(fullPath).AbsoluteUri;
                        var imageRef = new Dictionary<string, object?>
                        {
                            ["type"] = "image",
                            ["asset_id"] = null,
                            // Important: also set uri to the local path so local servers can read it directly.
                            // This makes the ref non-empty even if the runtime drops binary payloads in MessagePack.
                            ["uri"] = fileUri,
                            ["data"] = bytes
                        };
                        Console.WriteLine($"WorkflowNodeBase: ImageRef prepared (uri='{fileUri}', dataLen={bytes.Length})");
                        return imageRef;
                    }

                    // URL or already a server-side uri
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

                    // Best-effort fallback: treat as a path-like string even if File.Exists is false (e.g., path on server).
                    Console.WriteLine($"WorkflowNodeBase: Image input '{inputName}' path does not exist locally; sending uri as-is.");
                    return new Dictionary<string, object?>
                    {
                        ["type"] = "image",
                        ["asset_id"] = null,
                        ["uri"] = s,
                        ["data"] = null
                    };
                }
            }

            return rawValue == null
                ? ""
                : VlValueConversion.NormalizeForTransport(rawValue);
        }

        private static async Task<object> ConvertMediaInputAsync(
            string inputName,
            string mediaType,
            object? rawValue,
            CancellationToken cancellationToken)
        {
            mediaType = mediaType == "asset_ref" ? "asset" : mediaType;
            byte[]? bytes = rawValue as byte[];
            string? uriText = null;

            if (rawValue is AssetRef assetRef)
            {
                bytes = assetRef.Data as byte[];
                uriText = assetRef.Uri;
                if (bytes is { Length: > 0 } && ShouldUploadMedia(bytes.LongLength))
                {
                    return await UploadMediaBytesAsync(
                        inputName,
                        mediaType,
                        bytes,
                        GetDefaultExtension(mediaType),
                        GetDefaultContentType(mediaType),
                        cancellationToken);
                }

                if (!assetRef.IsEmpty())
                {
                    return new Dictionary<string, object?>
                    {
                        ["type"] = mediaType,
                        ["asset_id"] = assetRef.AssetId,
                        ["uri"] = assetRef.Uri,
                        ["data"] = assetRef.Data
                    };
                }
            }
            else if (rawValue is SKImage image)
            {
                using var encoded = image.Encode(SKEncodedImageFormat.Png, 100)
                    ?? throw new InvalidOperationException($"Could not encode image input '{inputName}'.");
                bytes = encoded.ToArray();
                if (ShouldUploadMedia(bytes.LongLength))
                    return await UploadMediaBytesAsync(inputName, mediaType, bytes, ".png", "image/png", cancellationToken);
            }
            else if (bytes == null)
            {
                uriText = rawValue switch
                {
                    string text => text.Trim().Trim('"'),
                    Uri uri => uri.ToString(),
                    null => null,
                    _ => rawValue.ToString()?.Trim()
                };

                if (!string.IsNullOrWhiteSpace(uriText))
                {
                    var fullPath = uriText;
                    try { fullPath = Path.GetFullPath(uriText); } catch { /* keep original URI */ }
                    if (File.Exists(fullPath))
                    {
                        var fileInfo = new FileInfo(fullPath);
                        if (ShouldUploadMedia(fileInfo.Length))
                            return await UploadMediaFileAsync(inputName, mediaType, fullPath, cancellationToken);

                        bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
                        uriText = new Uri(fullPath).AbsoluteUri;
                    }
                }
            }

            if ((bytes == null || bytes.Length == 0) && string.IsNullOrWhiteSpace(uriText))
            {
                throw new InvalidOperationException(
                    $"{mediaType} input '{inputName}' is empty. Provide a value, file path, URL, or bytes.");
            }

            if (bytes is { Length: > 0 } && ShouldUploadMedia(bytes.LongLength))
                return await UploadMediaBytesAsync(
                    inputName,
                    mediaType,
                    bytes,
                    GetDefaultExtension(mediaType),
                    GetDefaultContentType(mediaType),
                    cancellationToken);

            return new Dictionary<string, object?>
            {
                ["type"] = mediaType,
                ["asset_id"] = null,
                ["uri"] = uriText ?? "",
                ["data"] = bytes
            };
        }

        private static bool ShouldUploadMedia(long byteCount)
            => byteCount > NodeToolClientProvider.InlineMediaLimitBytes;

        private static async Task<object> UploadMediaFileAsync(
            string inputName,
            string mediaType,
            string path,
            CancellationToken cancellationToken)
        {
            await using var stream = File.OpenRead(path);
            return await UploadMediaStreamAsync(
                inputName,
                mediaType,
                Path.GetFileName(path),
                GetContentType(path, mediaType),
                stream,
                cancellationToken);
        }

        private static async Task<object> UploadMediaBytesAsync(
            string inputName,
            string mediaType,
            byte[] bytes,
            string extension,
            string contentType,
            CancellationToken cancellationToken)
        {
            using var stream = new MemoryStream(bytes, writable: false);
            return await UploadMediaStreamAsync(
                inputName,
                mediaType,
                $"vvvv-{mediaType}-{Guid.NewGuid():N}{extension}",
                contentType,
                stream,
                cancellationToken);
        }

        private static async Task<object> UploadMediaStreamAsync(
            string inputName,
            string mediaType,
            string fileName,
            string contentType,
            Stream stream,
            CancellationToken cancellationToken)
        {
            var apiBase = NodeToolClientProvider.CurrentApiBaseUrl
                ?? throw new InvalidOperationException(
                    $"Cannot upload large {mediaType} input '{inputName}': no HTTP API URL is configured.");

            using var httpClient = new HttpClient();
            var client = new NodetoolClient(httpClient);
            client.Configure(apiBase.ToString(), NodeToolClientProvider.CurrentAuthToken);
            var asset = await client.UploadAssetAsync(fileName, stream, contentType, cancellationToken);

            return new Dictionary<string, object?>
            {
                ["type"] = mediaType,
                ["asset_id"] = asset.Id,
                ["uri"] = asset.GetUrl ?? asset.Uri ?? "",
                ["data"] = null
            };
        }

        private static string GetContentType(string path, string mediaType)
            => Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                ".wav" => "audio/wav",
                ".mp3" => "audio/mpeg",
                ".ogg" => "audio/ogg",
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".pdf" => "application/pdf",
                _ => GetDefaultContentType(mediaType)
            };

        private static string GetDefaultContentType(string mediaType)
            => mediaType switch
            {
                "image" => "image/png",
                "audio" => "application/octet-stream",
                "video" => "application/octet-stream",
                "document" => "application/octet-stream",
                _ => "application/octet-stream"
            };

        private static string GetDefaultExtension(string mediaType)
            => mediaType == "image" ? ".png" : ".bin";

        private static bool IsImageSchema(WorkflowPropertyDefinition? prop, WorkflowSchemaDefinition? rootSchema)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            return IsImageSchemaInner(prop, rootSchema, visited, depth: 0);
        }

        private static bool IsImageSchemaInner(
            WorkflowPropertyDefinition? prop,
            WorkflowSchemaDefinition? rootSchema,
            HashSet<string> visitedRefs,
            int depth)
        {
            if (prop == null || depth > 10)
                return false;

            // Strong hint: explicit format
            if (string.Equals(prop.Format, "image", StringComparison.OrdinalIgnoreCase))
                return true;

            // Handle wrappers
            if (prop.AnyOf != null && prop.AnyOf.Any(p => IsImageSchemaInner(p, rootSchema, visitedRefs, depth + 1)))
                return true;
            if (prop.OneOf != null && prop.OneOf.Any(p => IsImageSchemaInner(p, rootSchema, visitedRefs, depth + 1)))
                return true;
            if (prop.AllOf != null && prop.AllOf.Any(p => IsImageSchemaInner(p, rootSchema, visitedRefs, depth + 1)))
                return true;

            // $ref resolution (the actual robust bit)
            if (!string.IsNullOrWhiteSpace(prop.Ref))
            {
                if (!visitedRefs.Add(prop.Ref))
                    return false;

                var resolved = ResolveRef(rootSchema, prop.Ref);
                if (resolved != null)
                    return IsImageSchemaInner(resolved, rootSchema, visitedRefs, depth + 1);

                // If we can't resolve (unexpected ref shape), treat as non-image (no heuristics here).
                return false;
            }

            // Object-ref shape: { type:"object", properties:{ type:{const:"image"}, ... } }
            if (string.Equals(prop.Type, "object", StringComparison.OrdinalIgnoreCase) && prop.Properties != null)
            {
                if (prop.Properties.TryGetValue("type", out var typeProp))
                {
                    if (typeProp.Const is string cs && string.Equals(cs, "image", StringComparison.OrdinalIgnoreCase))
                        return true;

                    if (typeProp.Enum != null && typeProp.Enum.Any(v => string.Equals(v?.ToString(), "image", StringComparison.OrdinalIgnoreCase)))
                        return true;
                }

                // AssetRef-ish object (uri/asset_id/data)
                if (prop.Properties.ContainsKey("uri") && prop.Properties.ContainsKey("asset_id"))
                    return true;
            }

            // Array of image refs
            if (string.Equals(prop.Type, "array", StringComparison.OrdinalIgnoreCase) && prop.Items != null)
                return IsImageSchemaInner(prop.Items, rootSchema, visitedRefs, depth + 1);

            return false;
        }

        private static WorkflowPropertyDefinition? ResolveRef(WorkflowSchemaDefinition? rootSchema, string refStr)
        {
            // Supports: "#/definitions/Name" and "#/$defs/Name"
            if (rootSchema == null)
                return null;

            if (!refStr.StartsWith("#/", StringComparison.Ordinal))
                return null;

            var path = refStr.Substring(2); // drop "#/"
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && string.Equals(parts[0], "definitions", StringComparison.Ordinal))
            {
                return rootSchema.Definitions != null && rootSchema.Definitions.TryGetValue(parts[1], out var def) ? def : null;
            }
            if (parts.Length == 2 && string.Equals(parts[0], "$defs", StringComparison.Ordinal))
            {
                return rootSchema.Defs != null && rootSchema.Defs.TryGetValue(parts[1], out var def) ? def : null;
            }

            return null;
        }

        private static void DumpSchemaForDebug(string inputName, WorkflowPropertyDefinition? propDef, WorkflowSchemaDefinition? rootSchema)
        {
            try
            {
                var propJson = propDef == null ? "<null>" : JsonSerializer.Serialize(propDef);
                Console.WriteLine($"WorkflowNodeBase: Input property schema '{inputName}': {propJson}");

                if (propDef?.Ref != null)
                {
                    var resolved = ResolveRef(rootSchema, propDef.Ref);
                    var resolvedJson = resolved == null ? "<unresolved>" : JsonSerializer.Serialize(resolved);
                    Console.WriteLine($"WorkflowNodeBase: Resolved $ref for '{inputName}' ({propDef.Ref}): {resolvedJson}");
                }
            }
            catch
            {
                // ignore
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Console.WriteLine($"WorkflowNodeBase: Disposing workflow node '{_workflow.Name}'");

                _isDisposed = true;
                try { _disposeCts.Cancel(); } catch { /* ignore */ }
                try { _manualCancelCts?.Cancel(); } catch { /* ignore */ }
                if (_activeSession is { } session)
                    _ = CancelSessionOnDisposeAsync(session);

                while (_pendingStateUpdates.TryDequeue(out _))
                {
                }

                foreach (var img in _latestImages.Values)
                {
                    try { img.Dispose(); } catch { /* ignore */ }
                }
                _latestImages.Clear();
                _latchedOutputValues.Clear();
                _disposeCts.Dispose();
            }
        }

        private static async Task CancelSessionOnDisposeAsync(IExecutionSession session)
        {
            try
            {
                await session.CancelAsync().ConfigureAwait(false);
            }
            catch
            {
                // Disposal is best effort and cannot report asynchronous failures to VL.
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
