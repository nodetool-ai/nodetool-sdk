using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;
using SkiaSharp;
using Nodetool.SDK.Assets;
using VL.Core;
using Nodetool.SDK.Execution;
using Nodetool.SDK.Types;
using Nodetool.SDK.Values;
using Nodetool.SDK.Workflows;
using Nodetool.SDK.Types.Assets;
using Nodetool.SDK.Utilities.Execution;
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
        private readonly OnInputChangeScheduler _inputChangeScheduler = new();
        private volatile bool _cancelRequestedByRestart = false;
        private bool _hasInitialized = false;
        private bool _prevAutoRunEnabled = false;

        private WorkflowExecutionRuntime? _executionRuntime;
        private volatile bool _isDisposed = false;
        private volatile bool _isRunning = false;
        private Task _executionTask = Task.CompletedTask;
        private readonly ConcurrentQueue<Action> _pendingStateUpdates = new();
        private readonly WorkflowOutputUpdateTracker _outputUpdateTracker = new();
        private readonly Dictionary<string, SKImage> _latestImages = new(StringComparer.Ordinal);
        private readonly Dictionary<string, object?> _latchedOutputValues = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, long> _imageLoadVersions = new(StringComparer.Ordinal);
        private readonly CancellationTokenSource _disposeCts = new();
        private readonly Queue<string> _debugLines = new();
        private const int DebugMaxLines = 30;
        private int _invalidateScheduled;

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
                var (vlType, typeDefault) = WorkflowVlTypeMapping.GetInputTypeAndDefault(property.Type);
                var defaultValue = property.DefaultValue != null 
                    ? ConvertToExpectedType(property.DefaultValue, vlType) 
                    : typeDefault;
                _inputPins[property.Name] = new InternalPin(property.Name, vlType, defaultValue);
            }
            
            // Create output pins
            _outputPins = new Dictionary<string, IVLPin>();
            
            // Add standard output pins
            _outputPins["IsRunning"] = new InternalPin("IsRunning", typeof(bool), false);
            _outputPins["Execution Time"] = new InternalPin(
                "Execution Time",
                typeof(TimeSpan),
                TimeSpan.Zero);
            _outputPins["Error"] = new InternalPin("Error", typeof(string), "");
            _outputPins["Debug"] = new InternalPin("Debug", typeof(string), "");
            // Add workflow output pins
            foreach (var property in _workflow.GetOutputProperties())
            {
                // Get consistent VL type and default value
                var (vlType, defaultValue) = WorkflowVlTypeMapping.GetTypeAndDefault(property.Type);
                _outputPins[property.Name] = new InternalPin(property.Name, vlType, defaultValue);
            }

            foreach (var output in _outputPins)
                _latchedOutputValues[output.Key] = output.Value.Value;

            VlLog.Debug(
                $"WorkflowNodeBase: created '{_workflow.Name}' with " +
                $"{_inputPins.Count} inputs and {_outputPins.Count} outputs");
        }

        public IVLPin[] Inputs => _inputPins.Values.ToArray();
        public IVLPin[] Outputs => _outputPins.Values.ToArray();

        // IVLNode implementation
        public IVLNodeDescription NodeDescription => _description;
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
                    _inputChangeScheduler.Reset(ComputeInputSignature());
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
                    _inputChangeScheduler.Reset(ComputeInputSignature());
                    StartExecution();
                }

                _lastTriggerState = currentTriggerState;

                // When AutoRun is turned on, just "arm" it (capture current signature) instead of running immediately.
                if (_autoRunEnabled && !_prevAutoRunEnabled)
                {
                    _inputChangeScheduler.Reset(ComputeInputSignature());
                    _prevAutoRunEnabled = true;
                    return;
                }
                _prevAutoRunEnabled = _autoRunEnabled;

                if (_autoRunEnabled)
                {
                    var sig = ComputeInputSignature();
                    switch (_inputChangeScheduler.NotifyInputs(
                        sig,
                        _isRunning,
                        _restartOnChangeEnabled))
                    {
                        case OnInputChangeAction.Start:
                            StartExecution();
                            break;
                        case OnInputChangeAction.CancelAndRestart:
                            _cancelRequestedByRestart = true;
                            _ = CancelActiveRunAsync();
                            break;
                    }
                }

                if (_autoRunEnabled &&
                    !_isRunning &&
                    _inputChangeScheduler.ConsumeRerunRequested())
                {
                    AppendDebug("autorun: rerun requested");
                    StartExecution();
                }
            }
            catch (Exception ex)
            {
                VlLog.Error(
                    $"WorkflowNodeBase: update failed: {VlLog.SafeError(ex)}");
                SetError($"Update error: {VlLog.SafeError(ex)}");
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
            var executionTimer = Stopwatch.StartNew();
            var timeoutSeconds = NodeToolClientProvider.ResolveExecutionTimeoutSeconds(
                _inputPins.TryGetValue("ExecutionTimeoutSeconds", out var timeoutPin) && timeoutPin.Value is int value ? value : 0);
            try
            {
                AppendDebug($"start workflow='{_workflow.Name}'");
                VlLog.Debug(
                    $"WorkflowNodeBase: starting '{_workflow.Name}'");
                // Help debug "changes not taking effect": print the actual loaded DLL + version at runtime.
                var asm = typeof(WorkflowNodeBase).Assembly;
                VlLog.Debug(
                    $"WorkflowNodeBase: assembly '{asm.Location}', " +
                    $"version={asm.GetName().Version}");

                _debugLines.Clear();
                if (_outputPins.TryGetValue("Debug", out var debugPin))
                    debugPin.Value = "";
                _cancelRequestedByRestart = false;

                SetIsRunning(true);
                SetError("");

                var runtime = GetOrCreateExecutionRuntime();
                runtime.InlineMediaLimitBytes =
                    NodeToolClientProvider.InlineMediaLimitBytes;
                var result = await runtime.ExecuteAsync(
                    CollectWorkflowInputs(),
                    TimeSpan.FromSeconds(timeoutSeconds),
                    retainOutputs: true,
                    executionOptions:
                        NodeToolClientProvider.ExecutionOptions,
                    cancellationToken: _disposeCts.Token);
                LogExecutionTiming(result.Timing);

                var terminal = result.Snapshot;
                if (terminal.State == WorkflowExecutionState.Completed)
                {
                    VlLog.Debug(
                        $"WorkflowNodeBase: completed '{_workflow.Name}'");
                    AppendDebug(
                        $"done total={result.Timing.Total.TotalMilliseconds:F0}ms");
                }
            }
            catch (TimeoutException)
            {
                SetError(
                    $"Execution timed out after {timeoutSeconds} seconds.");
                AppendDebug($"timed out after {timeoutSeconds}s");
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
                    var timedOut = _executionRuntime?.Snapshot.State ==
                        WorkflowExecutionState.TimedOut;
                    SetError(timedOut ? $"Execution timed out after {timeoutSeconds} seconds." : "Execution cancelled.");
                    AppendDebug(timedOut ? $"timed out after {timeoutSeconds}s" : "cancelled");
                    SetIsRunning(false);
                }
            }
            catch (Exception ex)
            {
                VlLog.Error(
                    $"WorkflowNodeBase: execution failed for " +
                    $"'{_workflow.Name}': {VlLog.SafeError(ex)}");
                SetError($"Execution failed: {VlLog.SafeError(ex)}");
                AppendDebug($"exception: {VlLog.SafeError(ex)}");
                SetIsRunning(false);
            }
            finally
            {
                executionTimer.Stop();
                SetExecutionTime(executionTimer.Elapsed);
                _isRunning = false;
            }
        }

        private void LogExecutionTiming(WorkflowExecutionTiming timing)
        {
            var message =
                $"connection={timing.Connection.TotalMilliseconds:F0}ms " +
                $"input={timing.InputPreparation.TotalMilliseconds:F0}ms " +
                $"remote={timing.RemoteExecution.TotalMilliseconds:F0}ms";
            AppendDebug(message);
            VlLog.Debug(
                $"WorkflowNodeBase: Workflow '{_workflow.Name}' {message}");
        }

        private async Task CancelActiveRunAsync()
        {
            if (_executionRuntime != null)
            {
                try
                {
                    await _executionRuntime.CancelAsync();
                }
                catch
                {
                    // ignore
                }
            }
        }

        private WorkflowExecutionRuntime GetOrCreateExecutionRuntime()
        {
            if (_executionRuntime != null)
                return _executionRuntime;

            var descriptor = _workflow.Descriptor
                ?? throw new InvalidOperationException(
                    $"Workflow '{_workflow.Name}' has no portable descriptor.");
            _executionRuntime = new WorkflowExecutionRuntime(
                new VlNodeToolExecutionConnection(),
                descriptor,
                NodeToolClientProvider.InlineMediaLimitBytes,
                adaptHostMediaValue:
                    VlMediaInputAdapter.AdaptValueAsync);
            _executionRuntime.SnapshotChanged += OnExecutionSnapshotChanged;
            return _executionRuntime;
        }

        private void OnExecutionSnapshotChanged(
            WorkflowExecutionSnapshot snapshot)
        {
            EnqueueStateUpdate(() => ApplyExecutionSnapshot(snapshot));
        }

        private void ApplyExecutionSnapshot(WorkflowExecutionSnapshot snapshot)
        {
            SetIsRunningCore(snapshot.State is
                WorkflowExecutionState.Starting or
                WorkflowExecutionState.Running or
                WorkflowExecutionState.Cancelling);

            if (!string.IsNullOrWhiteSpace(snapshot.Error) &&
                !(_cancelRequestedByRestart &&
                  snapshot.State == WorkflowExecutionState.Cancelled))
            {
                SetErrorCore(snapshot.Error);
            }

            foreach (var output in _outputUpdateTracker.SelectChanges(snapshot))
            {
                if (!_outputPins.TryGetValue(output.PublicName, out var pin))
                    continue;
                var expectedType = (pin as InternalPin)?.Type ?? typeof(string);
                if (expectedType == typeof(SKImage))
                {
                    ApplyOrScheduleImageOutput(
                        output.PublicName,
                        pin,
                        output.Value);
                    continue;
                }

                SetOutputPinValue(
                    output.PublicName,
                    pin,
                    ConvertNodeToolValueToExpectedType(
                        output.Value,
                        expectedType));
            }
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
                    VlLog.Error(
                        $"WorkflowNodeBase: queued state failed: " +
                        VlLog.SafeError(ex));
                    SetErrorCore($"State update failed: {VlLog.SafeError(ex)}");
                }
            }

            // Count is only a snapshot. Keep evaluating if producers added more work
            // while this frame was draining the queue.
            if (!_pendingStateUpdates.IsEmpty)
                RequestVlUpdate();
        }

        private void ApplyOrScheduleImageOutput(string pinName, IVLPin pin, NodeToolValue value)
        {
            if (!NodeToolAssetValueParser.TryParse(
                    value,
                    "image",
                    out var asset))
            {
                // Keep a valid progressive value if the terminal snapshot
                // contains only an incomplete reference.
                return;
            }

            if (NodeToolAssetValueParser.TryGetBytes(asset, out var bytes))
            {
                _imageLoadVersions.AddOrUpdate(pinName, 1, static (_, current) => current + 1);
                ApplyDecodedImage(pinName, pin, bytes);
                return;
            }

            var version = _imageLoadVersions.AddOrUpdate(pinName, 1, static (_, current) => current + 1);
            _ = LoadImageOutputAsync(
                pinName,
                pin,
                asset,
                version,
                _disposeCts.Token);
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
            VlLog.Debug(
                $"WorkflowNodeBase: image output ready: pin='{pinName}' size={image.Width}x{image.Height}");
        }

        private async Task LoadImageOutputAsync(
            string pinName,
            IVLPin pin,
            AssetRef asset,
            long version,
            CancellationToken cancellationToken)
        {
            try
            {
                var materialized = await AssetFileMaterializer.MaterializeAsync(
                    asset,
                    forceRefresh: false,
                    cancellationToken).ConfigureAwait(false);
                await using var stream = new FileStream(
                    materialized.Path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 81920,
                    useAsync: true);
                var image = SKImage.FromEncodedData(stream);

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
                        SetErrorCore($"Failed to load image output '{pinName}': {VlLog.SafeError(ex)}");
                    }
                });
            }
        }

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

        private void SetExecutionTime(TimeSpan executionTime)
        {
            EnqueueStateUpdate(() =>
            {
                if (_outputPins.TryGetValue("Execution Time", out var pin))
                    SetOutputPinValue("Execution Time", pin, executionTime);
            });
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
                return NodeToolValuePresentation.ToDisplayString(value);
            }

            if (typeof(AssetRef).IsAssignableFrom(expectedType))
            {
                return VlValueConversion.ConvertNodeToolValueToAssetRef(
                    value,
                    expectedType);
            }

            if (expectedType == typeof(object))
            {
                return NodeToolValuePresentation.ToDisplayObject(value) ?? "";
            }

            if (expectedType == typeof(int) && value.TryGetLong(out var l))
            {
                return checked((int)l);
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

        private Dictionary<string, object?> CollectWorkflowInputs()
        {
            var rawInputs = new Dictionary<string, object?>(
                StringComparer.Ordinal);
            foreach (var pin in _inputPins)
            {
                if (pin.Key is "Trigger" or "Cancel" or "AutoRun" or
                    "RestartOnChange" or "ExecutionTimeoutSeconds")
                {
                    continue;
                }
                rawInputs[pin.Key] =
                    VlValueConversion.NormalizeDynamicEnumsForTransport(
                        pin.Value.Value);
            }
            return rawInputs;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                VlLog.Debug(
                    $"WorkflowNodeBase: disposing '{_workflow.Name}'");

                _isDisposed = true;
                try { _disposeCts.Cancel(); } catch { /* ignore */ }
                if (_executionRuntime is { } runtime)
                {
                    runtime.SnapshotChanged -= OnExecutionSnapshotChanged;
                    _executionRuntime = null;
                    _ = DisposeRuntimeAsync(runtime);
                }

                while (_pendingStateUpdates.TryDequeue(out _))
                {
                }

                foreach (var img in _latestImages.Values)
                {
                    try { img.Dispose(); } catch { /* ignore */ }
                }
                _latestImages.Clear();
                _latchedOutputValues.Clear();
                _outputUpdateTracker.Reset();
                _disposeCts.Dispose();
            }
        }

        private static async Task DisposeRuntimeAsync(
            WorkflowExecutionRuntime runtime)
        {
            try
            {
                await runtime.DisposeAsync().ConfigureAwait(false);
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
