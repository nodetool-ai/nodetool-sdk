using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Reflection;
using SkiaSharp;
using VL.Core;
using Nodetool.SDK.Execution;
using Nodetool.SDK.Values;
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
        private const int DefaultWorkflowTimeoutSeconds = 300;

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
        private bool _cancelRequestedByRestart = false;
        private bool _hasInitialized = false;
        private bool _prevAutoRunEnabled = false;

        private IExecutionSession? _activeSession = null;
        private CancellationTokenSource? _manualCancelCts = null;
        private bool _isDisposed = false;
        private bool _isRunning = false;
        private readonly Dictionary<string, StringBuilder> _chunkBuffers = new(StringComparer.Ordinal);
        private readonly Dictionary<string, SKImage> _latestImages = new(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _latestAudioPaths = new(StringComparer.Ordinal);
        private readonly HashSet<string> _audioDownloadsInFlight = new(StringComparer.Ordinal);
        private readonly Queue<string> _debugLines = new();
        private const int DebugMaxLines = 30;

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
            
            // Add workflow input pins
            foreach (var property in _workflow.GetInputProperties())
            {
                var (vlType, defaultValue) = VlWorkflowTypeMapping.MapWorkflowType(
                    property.Name,
                    property.Type,
                    property.DefaultValue);
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
                var (vlType, defaultValue) = VlWorkflowTypeMapping.MapWorkflowType(
                    property.Name,
                    property.Type,
                    schemaDefaultValue: null);
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
                    ExecuteWorkflowAsync();
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
                            ExecuteWorkflowAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WorkflowNodeBase: Error in Update(): {ex.Message}");
                SetError($"Update error: {ex.Message}");
            }
        }

        private async void ExecuteWorkflowAsync()
        {
            if (_isRunning) return;
            _isRunning = true;
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

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(DefaultWorkflowTimeoutSeconds));
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

                // Execute by name (requires ApiBaseUrl on the shared client options)
                var session = await client.ExecuteWorkflowByNameAsync(_workflow.Name, parameters, linked.Token);
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
                    // Canonical mapping: for Output nodes, the backend sets output_name=node.name (e.g. "inverted").
                    var hasPin = _outputPins.TryGetValue(update.OutputName, out var pin);
                    Console.WriteLine(
                        $"WorkflowNodeBase: output_update received: output_name='{update.OutputName}' node_name='{update.NodeName}' output_type='{update.OutputType}' hasPin={hasPin}");
                    AppendDebug($"output_update: {update.OutputName} type={update.OutputType}");

                    if (pin != null)
                    {
                        // IVLPin doesn't expose Type; our InternalPin does.
                        var expectedType = (pin as InternalPin)?.Type ?? typeof(string);

                        // Special handling for streamed "chunk" payloads: accumulate content so the pin shows useful text.
                        if (update.Value.Kind == NodeToolValueKind.Map)
                        {
                            var map = update.Value.AsMapOrEmpty();
                            var typeDisc = update.Value.TypeDiscriminator;
                            if (string.Equals(typeDisc, "chunk", StringComparison.OrdinalIgnoreCase))
                            {
                                var content = map.TryGetValue("content", out var c) ? (c.AsString() ?? "") : "";
                                var done = map.TryGetValue("done", out var d) && d.TryGetBool(out var b) && b;

                                if (!_chunkBuffers.TryGetValue(update.OutputName, out var sb))
                                {
                                    sb = new StringBuilder();
                                    _chunkBuffers[update.OutputName] = sb;
                                }

                                if (!string.IsNullOrEmpty(content))
                                {
                                    sb.Append(content);
                                }

                                // Show accumulated content even when we get the final done=true message (often empty content).
                                pin.Value = sb.ToString();
                                return;
                            }
                        }

                        // Special handling for image outputs:
                        // Avoid JSON and avoid writing temp files. Prefer returning the encoded image bytes.
                        if (string.Equals(update.OutputType, "image", StringComparison.OrdinalIgnoreCase))
                        {
                            if (TryExtractImageBytes(update.Value, out var bytes) && bytes.Length > 0)
                            {
                                // Decode to SKImage (no temp files, no JSON)
                                var img = SKImage.FromEncodedData(bytes);
                                if (img != null)
                                {
                                    if (_latestImages.TryGetValue(update.OutputName, out var prev))
                                    {
                                        prev.Dispose();
                                    }
                                    _latestImages[update.OutputName] = img;
                                    pin.Value = img;
                                    return;
                                }

                                SetError($"Failed to decode image bytes for output '{update.OutputName}'.");
                                return;
                            }
                        }

                        // Special handling for audio outputs: expose a local cached file path string.
                        if (string.Equals(update.OutputType, "audio", StringComparison.OrdinalIgnoreCase) &&
                            TryExtractAssetUri(update.Value, expectedType: "audio", out var audioUri))
                        {
                            var normalized = NodeToolClientProvider.NormalizeAssetUri(audioUri);

                            if (_latestAudioPaths.TryGetValue(update.OutputName, out var cached) &&
                                !string.IsNullOrWhiteSpace(cached) &&
                                File.Exists(cached))
                            {
                                pin.Value = cached;
                                return;
                            }

                            try
                            {
                                var assets = NodeToolClientProvider.GetAssetManager();
                                var hit = assets.GetCachedPath(normalized);
                                if (!string.IsNullOrWhiteSpace(hit) && File.Exists(hit))
                                {
                                    _latestAudioPaths[update.OutputName] = hit;
                                    pin.Value = hit;
                                    return;
                                }

                                // Show URI while downloading; replace once done.
                                pin.Value = normalized;
                                if (_audioDownloadsInFlight.Add(update.OutputName))
                                {
                                    _ = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            var path = await assets.DownloadAssetAsync(normalized, cancellationToken: CancellationToken.None);
                                            _latestAudioPaths[update.OutputName] = path;
                                        }
                                        catch (Exception ex)
                                        {
                                            SetError($"Audio download failed ({update.OutputName}): {ex.Message}");
                                        }
                                        finally
                                        {
                                            _audioDownloadsInFlight.Remove(update.OutputName);
                                            if (_outputPins.TryGetValue(update.OutputName, out var p) &&
                                                _latestAudioPaths.TryGetValue(update.OutputName, out var finalPath))
                                            {
                                                p.Value = finalPath;
                                            }
                                        }
                                    });
                                }

                                return;
                            }
                            catch
                            {
                                // fall through to default conversion
                            }
                        }

                        pin.Value = ConvertNodeToolValueToExpectedType(update.Value, expectedType);
                    }
                };

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
                    SetError("Execution cancelled.");
                    AppendDebug("cancelled");
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
                _activeSession = null;
                _isRunning = false;

                // If inputs changed during execution and AutoRun is enabled, run once more.
                if (_autoRunEnabled && _rerunRequested && !_isDisposed)
                {
                    _rerunRequested = false;
                    AppendDebug("autorun: rerun requested");
                    ExecuteWorkflowAsync();
                }
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

        private string ComputeInputSignature()
        {
            // Cheap stable signature to detect input changes; excludes Trigger/AutoRun.
            var sb = new StringBuilder();
            foreach (var kvp in _inputPins.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                if (kvp.Key is "Trigger" or "Cancel" or "AutoRun" or "RestartOnChange")
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

        private void AppendDebug(string line)
        {
            try
            {
                var ts = DateTime.Now.ToString("HH:mm:ss.fff");
                var msg = $"{ts} {line}";

                while (_debugLines.Count >= DebugMaxLines)
                    _debugLines.Dequeue();
                _debugLines.Enqueue(msg);

                if (_outputPins.TryGetValue("Debug", out var pin))
                {
                    pin.Value = string.Join(Environment.NewLine, _debugLines);
                }
            }
            catch
            {
                // ignore debug failures
            }
        }

        private void ApplyFinalOutputsFromSession(IExecutionSession session)
        {
            try
            {
                var outputs = session.GetLatestOutputs();
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
                        // job_update.result omits inline bytes (this happens in some server modes).
                        if (expectedType == typeof(SKImage))
                        {
                            if (TryExtractImageBytes(kvp.Value, out var bytes) && bytes.Length > 0)
                            {
                                var img = SKImage.FromEncodedData(bytes);
                                if (img != null)
                                {
                                    if (_latestImages.TryGetValue(outputName, out var prev))
                                    {
                                        prev.Dispose();
                                    }
                                    _latestImages[outputName] = img;
                                    pin.Value = img;
                                    continue;
                                }
                                SetError($"Failed to decode image bytes for output '{outputName}'.");
                            }

                            // Keep existing pin.Value (likely set during output_update) if we can't decode a final image.
                            continue;
                        }

                        // Final audio: turn AudioRef into a local cached file path (string pin).
                        if (expectedType == typeof(string) &&
                            TryExtractAssetUri(kvp.Value, expectedType: "audio", out var audioUri))
                        {
                            try
                            {
                                var normalized = NodeToolClientProvider.NormalizeAssetUri(audioUri);
                                var assets = NodeToolClientProvider.GetAssetManager();

                                if (_latestAudioPaths.TryGetValue(outputName, out var already) &&
                                    !string.IsNullOrWhiteSpace(already) &&
                                    File.Exists(already))
                                {
                                    pin.Value = already;
                                    continue;
                                }

                                var hit = assets.GetCachedPath(normalized);
                                if (!string.IsNullOrWhiteSpace(hit) && File.Exists(hit))
                                {
                                    _latestAudioPaths[outputName] = hit;
                                    pin.Value = hit;
                                    continue;
                                }

                                // Show URI while downloading.
                                pin.Value = normalized;
                                if (_audioDownloadsInFlight.Add(outputName))
                                {
                                    _ = Task.Run(async () =>
                                    {
                                        try
                                        {
                                            var path = await assets.DownloadAssetAsync(normalized, cancellationToken: CancellationToken.None);
                                            _latestAudioPaths[outputName] = path;
                                        }
                                        catch (Exception ex)
                                        {
                                            SetError($"Audio download failed ({outputName}): {ex.Message}");
                                        }
                                        finally
                                        {
                                            _audioDownloadsInFlight.Remove(outputName);
                                            if (_outputPins.TryGetValue(outputName, out var p) &&
                                                _latestAudioPaths.TryGetValue(outputName, out var finalPath))
                                            {
                                                p.Value = finalPath;
                                            }
                                        }
                                    });
                                }

                                continue;
                            }
                            catch
                            {
                                // fall through to default conversion
                            }
                        }

                        pin.Value = ConvertNodeToolValueToExpectedType(kvp.Value, expectedType);
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

        private static bool TryExtractAssetUri(NodeToolValue value, string expectedType, out string uri)
        {
            uri = "";
            var map = ExtractFirstMap(value);
            if (map == null)
                return false;

            if (map.TryGetValue("type", out var typeVal) && typeVal.AsString() is string typeStr &&
                string.Equals(typeStr, expectedType, StringComparison.OrdinalIgnoreCase) &&
                map.TryGetValue("uri", out var uriVal) && uriVal.AsString() is string u && !string.IsNullOrWhiteSpace(u))
            {
                uri = u;
                return true;
            }

            if (string.Equals(value.TypeDiscriminator, expectedType, StringComparison.OrdinalIgnoreCase) &&
                map.TryGetValue("uri", out var uriVal2) && uriVal2.AsString() is string u2 && !string.IsNullOrWhiteSpace(u2))
            {
                uri = u2;
                return true;
            }

            return false;
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
                // data may come through as [137,80,78,71,...]
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

                    var ticks = microsecond > 0 ? microsecond * 10L : 0L; // 1 microsecond = 10 ticks
                    dtUtc = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc).AddTicks(ticks);
                    return true;
                }

                // Some server paths may use ISO string wrappers
                if (map.TryGetValue("value", out var inner) && inner.AsString() is string iso &&
                    DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
                {
                    dtUtc = DateTime.SpecifyKind(parsed.ToUniversalTime(), DateTimeKind.Utc);
                    return true;
                }
            }

            // Raw string fallback
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

            // microsecond precision (DateTime ticks are 100ns)
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

        // Type mapping is centralized in VlWorkflowTypeMapping (uses backend TypeMetadata).

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

        private static object ConvertNodeToolValueToExpectedType(NodeToolValue value, Type expectedType)
        {
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

            if (expectedType == typeof(DateTime))
            {
                return TryExtractDateTimeUtc(value, out var dtUtc) ? dtUtc : default(DateTime);
            }

            if (expectedType.IsEnum)
            {
                if (TryExtractEnumLiteral(value, out var literal) && literal is string litStr &&
                    Enum.TryParse(expectedType, litStr, ignoreCase: true, out var litParsed))
                    return litParsed!;

                if (value.AsString() is string s && Enum.TryParse(expectedType, s, ignoreCase: true, out var parsed))
                    return parsed!;

                if (value.TryGetLong(out var enumLong))
                    return Enum.ToObject(expectedType, (int)enumLong);
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

            // Array outputs: render as JSON string array when possible.
            if (expectedType == typeof(string[]))
            {
                if (value.Kind == NodeToolValueKind.List)
                {
                    return value.AsListOrEmpty().Select(v => v.AsString() ?? v.ToJsonString()).ToArray();
                }
                return new[] { value.AsString() ?? value.ToJsonString() };
            }

            return ConvertToExpectedType(value.Raw ?? value.AsString() ?? value.ToJsonString(), expectedType);
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
                // A typed list wrapper; handle chunk lists specially.
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
                if (kvp.Key is "Trigger" or "Cancel" or "AutoRun" or "RestartOnChange")
                    continue;

                var raw = kvp.Value.Value;
                parameters[kvp.Key] = await ConvertInputValueForWorkflowAsync(kvp.Key, raw, cancellationToken);
            }

            return parameters;
        }

        private async Task<object> ConvertInputValueForWorkflowAsync(string inputName, object? rawValue, CancellationToken cancellationToken)
        {
            // If we don't have schema, pass through as string for compatibility with TEST_SDK_01.
            var propDef = _workflow.InputSchema?.Properties != null && _workflow.InputSchema.Properties.TryGetValue(inputName, out var p)
                ? p
                : null;

            // Preferred (Phase 2): backend-provided TypeMetadata for runtime conversion.
            if (_workflow.TryGetInputType(inputName, out var tm))
            {
                var tt = (tm.Type ?? "any").Trim().ToLowerInvariant();

                if (tt == "datetime")
                {
                    if (rawValue == null)
                        return tm.Optional ? null! : "";

                    if (rawValue is DateTime dt)
                        return ToNodeToolDateTime(dt);

                    if (rawValue is DateTimeOffset dto)
                        return ToNodeToolDateTime(dto.UtcDateTime);

                    if (rawValue is string s &&
                        DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
                        return ToNodeToolDateTime(DateTime.SpecifyKind(parsed.ToUniversalTime(), DateTimeKind.Utc));

                    return rawValue.ToString() ?? "";
                }

                if (tt == "enum")
                {
                    if (rawValue is Enum e)
                        return StaticEnumRegistry.ToNodeToolLiteral(e);

                    return rawValue ?? "";
                }
            }

            // Schema-based detection only (no name heuristics).
            // This must handle $ref / anyOf / oneOf / allOf properly.
            var treatAsImage = (_workflow.TryGetInputType(inputName, out var tm2) &&
                                string.Equals(tm2.Type, "image", StringComparison.OrdinalIgnoreCase))
                               || IsImageSchema(propDef, _workflow.InputSchema);

            if (treatAsImage)
            {
                var rawType = rawValue?.GetType().FullName ?? "<null>";

                // Accept SKImage directly: encode to PNG and send as inline bytes.
                if (rawValue is SKImage skImage)
                {
                    Console.WriteLine($"WorkflowNodeBase: Image input '{inputName}' received SKImage ({skImage.Width}x{skImage.Height})");
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

            var treatAsAudio = (_workflow.TryGetInputType(inputName, out var tm3) &&
                                string.Equals(tm3.Type, "audio", StringComparison.OrdinalIgnoreCase));

            if (treatAsAudio)
            {
                // VL pin type is string. Convert file path / URL into an AudioRef-like payload.
                if (rawValue is byte[] bytesDirect)
                {
                    return new Dictionary<string, object?>
                    {
                        ["type"] = "audio",
                        ["asset_id"] = null,
                        ["uri"] = "",
                        ["data"] = bytesDirect
                    };
                }

                var s = rawValue switch
                {
                    null => null,
                    string str => str,
                    Uri u => u.ToString(),
                    _ => rawValue.ToString()
                };

                s = s?.Trim();
                if (string.IsNullOrWhiteSpace(s))
                    throw new InvalidOperationException($"Audio input '{inputName}' is empty. Provide a file path/URL.");

                var fullPath = s;
                try { fullPath = Path.GetFullPath(s); } catch { /* ignore */ }

                if (!string.IsNullOrWhiteSpace(fullPath) && File.Exists(fullPath))
                {
                    var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
                    return new Dictionary<string, object?>
                    {
                        ["type"] = "audio",
                        ["asset_id"] = null,
                        ["uri"] = new Uri(fullPath).AbsoluteUri,
                        ["data"] = bytes
                    };
                }

                if (Uri.TryCreate(s, UriKind.Absolute, out var uri))
                {
                    return new Dictionary<string, object?>
                    {
                        ["type"] = "audio",
                        ["asset_id"] = null,
                        ["uri"] = uri.ToString(),
                        ["data"] = null
                    };
                }

                return new Dictionary<string, object?>
                {
                    ["type"] = "audio",
                    ["asset_id"] = null,
                    ["uri"] = s,
                    ["data"] = null
                };
            }

            return rawValue ?? "";
        }

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

                foreach (var img in _latestImages.Values)
                {
                    try { img.Dispose(); } catch { /* ignore */ }
                }
                _latestImages.Clear();

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