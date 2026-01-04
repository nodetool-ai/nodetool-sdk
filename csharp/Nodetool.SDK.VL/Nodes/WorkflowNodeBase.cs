using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Reflection;
using VL.Core;
using Nodetool.SDK.Execution;
using Nodetool.SDK.Values;
using Nodetool.SDK.VL.Models;
using Nodetool.SDK.VL.Services;

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
        private bool _isDisposed = false;
        private bool _isRunning = false;
        private readonly Dictionary<string, StringBuilder> _chunkBuffers = new(StringComparer.Ordinal);

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
            if (_isRunning) return;
            _isRunning = true;
            try
            {
                Console.WriteLine($"WorkflowNodeBase: Starting execution of workflow '{_workflow.Name}'");
                // Help debug "changes not taking effect": print the actual loaded DLL + version at runtime.
                var asm = typeof(WorkflowNodeBase).Assembly;
                Console.WriteLine($"WorkflowNodeBase: Using assembly '{asm.Location}', version={asm.GetName().Version}");

                // Reset per-run chunk buffers so streaming output doesn't accumulate across runs.
                _chunkBuffers.Clear();

                SetIsRunning(true);
                SetError("");

                if (_workflow.InputSchema?.Properties != null && _workflow.InputSchema.Properties.Count > 0)
                {
                    var keys = string.Join(", ", _workflow.InputSchema.Properties.Keys);
                    Console.WriteLine($"WorkflowNodeBase: Input schema keys: {keys}");
                }

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(DefaultWorkflowTimeoutSeconds));

                // Ensure connection
                if (!NodeToolClientProvider.IsConnected)
                {
                    var connected = await NodeToolClientProvider.ConnectAsync(timeoutCts.Token);
                    if (!connected)
                    {
                        throw new InvalidOperationException(NodeToolClientProvider.LastError ?? "Failed to connect to NodeTool server.");
                    }
                }

                var client = NodeToolClientProvider.GetClient();

                // Collect inputs from pins (excluding Trigger) and adapt values based on workflow schema.
                var parameters = await BuildWorkflowParametersAsync(timeoutCts.Token);

                // Execute by name (requires ApiBaseUrl on the shared client options)
                var session = await client.ExecuteWorkflowByNameAsync(_workflow.Name, parameters, timeoutCts.Token);

                session.ProgressChanged += progress =>
                {
                    // Lightweight progress trace (helps diagnose "runs forever")
                    Console.WriteLine($"WorkflowNodeBase: Workflow '{_workflow.Name}' progress: {progress:P0}");
                };

                session.NodeUpdated += update =>
                {
                    if (!string.IsNullOrWhiteSpace(update.error))
                    {
                        Console.WriteLine($"WorkflowNodeBase: Node error in workflow '{_workflow.Name}': {update.error}");
                        SetError(update.error);
                    }
                };

                session.Completed += (success, err) =>
                {
                    if (!success)
                    {
                        Console.WriteLine($"WorkflowNodeBase: Workflow '{_workflow.Name}' completed with error: {err}");
                        SetError(err ?? "Workflow failed.");
                    }
                };

                session.OutputReceived += update =>
                {
                    // Best-effort mapping: match output pin by output name
                    if (_outputPins.TryGetValue(update.OutputName, out var pin))
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

                        pin.Value = ConvertNodeToolValueToExpectedType(update.Value, expectedType);
                    }
                };

                var ok = await session.WaitForCompletionAsync(timeoutCts.Token);
                if (!ok)
                {
                    throw new InvalidOperationException(session.ErrorMessage ?? "Workflow execution failed.");
                }

                Console.WriteLine($"WorkflowNodeBase: Workflow '{_workflow.Name}' execution completed");
                SetIsRunning(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WorkflowNodeBase: Error executing workflow '{_workflow.Name}': {ex.Message}");
                SetError($"Execution failed: {ex.Message}");
                SetIsRunning(false);
            }
            finally
            {
                _isRunning = false;
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

        private static object ConvertNodeToolValueToExpectedType(NodeToolValue value, Type expectedType)
        {
            // Prefer primitives when possible; fall back to JSON string for complex values.
            if (expectedType == typeof(string))
            {
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
                    return value.ToJsonString();

                return value.AsString() ?? value.ToJsonString();
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
                return value.Raw;
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

        private async Task<Dictionary<string, object>> BuildWorkflowParametersAsync(CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<string, object>(StringComparer.Ordinal);

            foreach (var kvp in _inputPins)
            {
                if (kvp.Key == "Trigger")
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