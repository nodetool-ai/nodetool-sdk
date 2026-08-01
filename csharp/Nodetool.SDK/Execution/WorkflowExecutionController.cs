using System.Collections.ObjectModel;
using System.Text;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Diagnostics;
using Nodetool.SDK.Values;
using Nodetool.SDK.Workflows;

namespace Nodetool.SDK.Execution;

/// <summary>
/// Owns one host-neutral workflow invocation at a time.
///
/// The controller consumes the existing job-scoped execution session, routes
/// output-node updates to public workflow output names, accumulates streaming
/// text chunks, and reconciles live values with the terminal result snapshot.
/// It never invokes a host main thread or creates host-specific objects.
/// </summary>
public sealed class WorkflowExecutionController : IDisposable, IAsyncDisposable
{
    private readonly INodeToolExecutionClient _client;
    private readonly Func<CancellationToken, Task<SdkCapabilitiesResponse>>?
        _getCapabilities;
    private readonly Dictionary<string, string> _routes;
    private readonly object _gate = new();
    private readonly Dictionary<string, StringBuilder> _chunkBuffers =
        new(StringComparer.Ordinal);
    private WorkflowExecutionSnapshot _snapshot = WorkflowExecutionSnapshot.Idle;
    private IExecutionSession? _session;
    private CancellationTokenSource? _runCancellation;
    private CancellationTokenSource? _timeoutCancellation;
    private Task _monitorTask = Task.CompletedTask;
    private TaskCompletionSource<WorkflowExecutionSnapshot> _quiescentCompletion =
        CompletedCompletion(WorkflowExecutionSnapshot.Idle);
    private WorkflowInvocation? _pendingInvocation;
    private bool _startReserved;
    private bool _remoteCancelIssued;
    private bool _disposed;

    public WorkflowExecutionController(
        INodeToolExecutionClient client,
        IEnumerable<WorkflowOutputDescriptor> outputs,
        Func<CancellationToken, Task<SdkCapabilitiesResponse>>?
            getCapabilities = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _getCapabilities = getCapabilities;
        ArgumentNullException.ThrowIfNull(outputs);
        _routes = BuildRoutes(outputs);
    }

    public event Action<WorkflowExecutionSnapshot>? SnapshotChanged;

    /// <summary>
    /// Raw streamed content from the active execution. Callbacks run on the
    /// transport receive path; hosts must marshal to their own main thread.
    /// </summary>
    public event Action<ExecutionStreamUpdate>? StreamReceived;

    public WorkflowExecutionSnapshot Snapshot
    {
        get
        {
            lock (_gate)
                return _snapshot;
        }
    }

    public async Task<WorkflowExecutionSnapshot> StartAsync(
        WorkflowInvocation invocation,
        CancellationToken cancellationToken = default)
    {
        ValidateInvocation(invocation);

        lock (_gate)
        {
            ThrowIfDisposed();
            if (IsBusy())
                throw new InvalidOperationException(
                    "The workflow execution controller is already active.");
            _pendingInvocation = null;
            _startReserved = true;
            _quiescentCompletion =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        return await StartCoreAsync(invocation, cancellationToken);
    }

    /// <summary>
    /// Starts an invocation or coalesces it with an active run according to the
    /// requested policy. Queueing always keeps the latest requested invocation.
    /// </summary>
    public async Task<WorkflowExecutionSnapshot> RequestStartAsync(
        WorkflowInvocation invocation,
        WorkflowStartPolicy policy,
        CancellationToken cancellationToken = default)
    {
        ValidateInvocation(invocation);

        var cancelCurrent = false;
        var startNow = false;
        lock (_gate)
        {
            ThrowIfDisposed();
            if (!IsBusy())
            {
                _pendingInvocation = null;
                _startReserved = true;
                _quiescentCompletion =
                    new(TaskCreationOptions.RunContinuationsAsynchronously);
                startNow = true;
            }
            else
            {
                if (policy == WorkflowStartPolicy.Reject)
                {
                    throw new InvalidOperationException(
                        "The workflow execution controller is already active.");
                }

                _pendingInvocation = invocation;
                cancelCurrent =
                    policy == WorkflowStartPolicy.CancelAndRestart &&
                    _snapshot.State != WorkflowExecutionState.Cancelling;
            }
        }

        if (!startNow)
        {
            if (cancelCurrent)
                await CancelCurrentAsync(clearPending: false, cancellationToken);
            return Snapshot;
        }

        return await StartCoreAsync(invocation, cancellationToken);
    }

    private async Task<WorkflowExecutionSnapshot> StartCoreAsync(
        WorkflowInvocation invocation,
        CancellationToken cancellationToken)
    {
        CancellationToken runToken;
        IReadOnlyDictionary<string, WorkflowOutputState> outputs;
        lock (_gate)
        {
            if (_disposed)
                return _snapshot;
            CleanupRunState();
            _chunkBuffers.Clear();
            _remoteCancelIssued = false;
            outputs = invocation.RetainOutputs
                ? _snapshot.Outputs
                : EmptyOutputs();
            _timeoutCancellation = invocation.Timeout is { } timeout
                ? new CancellationTokenSource(timeout)
                : null;
            _runCancellation = _timeoutCancellation == null
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    _timeoutCancellation.Token);
            runToken = _runCancellation.Token;
        }

        Publish(new WorkflowExecutionSnapshot(
            WorkflowExecutionState.Starting,
            invocation.WorkflowId,
            null,
            0,
            outputs,
            null,
            DateTimeOffset.UtcNow,
            null));
        lock (_gate)
            _startReserved = false;

        try
        {
            var inputs = invocation.Inputs.ToDictionary(
                pair => pair.Key,
                pair => pair.Value!,
                StringComparer.Ordinal);
            var executionOptions = await ResolveExecutionOptionsAsync(
                invocation.ExecutionOptions,
                runToken);
            var session = executionOptions is null
                ? await _client.ExecuteWorkflowAsync(
                    invocation.WorkflowId,
                    inputs,
                    runToken)
                : await _client.ExecuteWorkflowAsync(
                    invocation.WorkflowId,
                    inputs,
                    executionOptions,
                    runToken);

            var disposed = false;
            lock (_gate)
            {
                disposed = _disposed;
                if (!disposed)
                {
                    _session = session;
                    Attach(session);
                }
            }
            if (disposed)
            {
                await CancelSessionOnceAsync(session, CancellationToken.None);
                session.Dispose();
                return Snapshot;
            }

            Update(current => current with
            {
                State = WorkflowExecutionState.Running,
                JobId = session.JobId
            });

            lock (_gate)
                _monitorTask = MonitorAsync(session, runToken);
            if (runToken.IsCancellationRequested)
            {
                try
                {
                    await CancelSessionOnceAsync(
                        session,
                        CancellationToken.None);
                }
                catch
                {
                    // The monitor still publishes the local terminal state.
                }
            }
            return Snapshot;
        }
        catch (OperationCanceledException) when (runToken.IsCancellationRequested)
        {
            PublishCancellationTerminal();
            await CompleteRunAndStartPendingAsync(null);
            return Snapshot;
        }
        catch (Exception exception)
        {
            PublishTerminal(
                WorkflowExecutionState.Failed,
                NodeToolDiagnosticRedactor.RedactText(exception.Message));
            await CompleteRunAndStartPendingAsync(null);
            return Snapshot;
        }
    }

    private async Task<WorkflowExecutionOptions?> ResolveExecutionOptionsAsync(
        WorkflowExecutionOptions? options,
        CancellationToken cancellationToken)
    {
        if (options is null ||
            WorkflowExecutionOptionNegotiator.IsDefault(options))
        {
            return null;
        }
        if (_getCapabilities is null)
        {
            throw new NotSupportedException(
                "Non-default execution options require SDK capability " +
                "negotiation before submission.");
        }

        var capabilities = await _getCapabilities(cancellationToken);
        WorkflowExecutionOptionNegotiator.EnsureSupported(
            capabilities,
            options);
        return options;
    }

    public async Task CancelAsync(CancellationToken cancellationToken = default)
        => await CancelCurrentAsync(clearPending: true, cancellationToken);

    /// <summary>
    /// Streams one value into the active workflow.
    /// </summary>
    public Task StreamInputAsync(
        string inputName,
        object? value,
        string? sourceHandle = null,
        CancellationToken cancellationToken = default)
        => GetActiveSession().StreamInputAsync(
            inputName,
            value,
            sourceHandle,
            cancellationToken);

    /// <summary>
    /// Marks one active workflow input stream as complete.
    /// </summary>
    public Task EndInputStreamAsync(
        string inputName,
        string? sourceHandle = null,
        CancellationToken cancellationToken = default)
        => GetActiveSession().EndInputStreamAsync(
            inputName,
            sourceHandle,
            cancellationToken);

    /// <summary>
    /// Applies properties to a node executor in the active workflow.
    /// </summary>
    public Task UpdateNodePropertiesAsync(
        string nodeId,
        IReadOnlyDictionary<string, object?> properties,
        CancellationToken cancellationToken = default)
        => GetActiveSession().UpdateNodePropertiesAsync(
            nodeId,
            properties,
            cancellationToken);

    private async Task CancelCurrentAsync(
        bool clearPending,
        CancellationToken cancellationToken)
    {
        IExecutionSession? session;
        lock (_gate)
        {
            ThrowIfDisposed();
            if (clearPending)
                _pendingInvocation = null;
            if (!IsActive(_snapshot.State))
            {
                return;
            }
            session = _session;
        }

        Update(current => current with
        {
            State = WorkflowExecutionState.Cancelling
        });
        _runCancellation?.Cancel();
        if (session != null)
        {
            try
            {
                await CancelSessionOnceAsync(session, cancellationToken);
            }
            catch (Exception exception)
            {
                Update(current => current with
                {
                    Error =
                        $"Remote cancellation failed: " +
                        NodeToolDiagnosticRedactor.RedactText(
                            exception.Message)
                });
            }
        }
    }

    public async Task WaitForTerminalAsync(
        CancellationToken cancellationToken = default)
    {
        Task<WorkflowExecutionSnapshot> completion;
        lock (_gate)
            completion = _quiescentCompletion.Task;
        await completion.WaitAsync(cancellationToken);
    }

    private async Task MonitorAsync(
        IExecutionSession session,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await session.WaitForCompletionAsync(cancellationToken);
            ReconcileTerminalOutputs(session.GetLatestOutputs());
            if (success)
            {
                PublishTerminal(WorkflowExecutionState.Completed, null);
            }
            else
            {
                var state = string.Equals(
                    session.CurrentStatus,
                    "cancelled",
                    StringComparison.OrdinalIgnoreCase)
                        ? WorkflowExecutionState.Cancelled
                        : WorkflowExecutionState.Failed;
                PublishTerminal(
                    state,
                    session.ErrorMessage is { } sessionError
                        ? NodeToolDiagnosticRedactor.RedactText(
                            sessionError)
                        : state == WorkflowExecutionState.Cancelled
                            ? "Execution cancelled."
                            : "Workflow execution failed.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            try
            {
                await CancelSessionOnceAsync(
                    session,
                    CancellationToken.None);
            }
            catch
            {
                // The terminal snapshot still reports local cancellation.
            }
            PublishCancellationTerminal();
        }
        catch (Exception exception)
        {
            PublishTerminal(
                WorkflowExecutionState.Failed,
                NodeToolDiagnosticRedactor.RedactText(exception.Message));
        }
        finally
        {
            await CompleteRunAndStartPendingAsync(session);
        }
    }

    private async Task CompleteRunAndStartPendingAsync(
        IExecutionSession? completedSession)
    {
        WorkflowInvocation? pending;
        TaskCompletionSource<WorkflowExecutionSnapshot>? completion = null;
        lock (_gate)
        {
            if (completedSession != null &&
                ReferenceEquals(_session, completedSession))
            {
                Detach(completedSession);
                completedSession.Dispose();
                _session = null;
            }

            pending = _disposed ? null : _pendingInvocation;
            _pendingInvocation = null;
            _startReserved = pending != null;
            if (pending == null)
                completion = _quiescentCompletion;
        }

        if (pending != null)
        {
            await StartCoreAsync(pending, CancellationToken.None);
            return;
        }

        completion?.TrySetResult(Snapshot);
    }

    private void OnProgressChanged(float progress)
        => Update(current => current with
        {
            Progress = Math.Clamp(progress, 0, 1)
        });

    private void OnNodeUpdated(Types.NodeUpdate update)
    {
        if (!string.IsNullOrWhiteSpace(update.error))
            Update(current => current with { Error = update.error });
    }

    private void OnOutputReceived(ExecutionOutputUpdate update)
    {
        var publicName = ResolvePublicName(update);
        if (publicName == null)
            return;

        var value = Accumulate(publicName, update);
        var isStreaming = IsTextStreamUpdate(update);
        Update(current =>
        {
            var outputs = new Dictionary<string, WorkflowOutputState>(
                current.Outputs,
                StringComparer.Ordinal)
            {
                [publicName] = new(
                    publicName,
                    value,
                    isStreaming,
                    update.Done,
                    update.ReceivedAt)
            };
            return current with { Outputs = ReadOnly(outputs) };
        });
    }

    private void OnStreamReceived(ExecutionStreamUpdate update)
    {
        if (update.Source == ExecutionStreamSource.StandaloneChunk &&
            IsTextContent(update.ContentType, update.Content) &&
            ResolvePublicName(update) is { } publicName)
        {
            var value = AccumulateText(
                publicName,
                update.Content.AsString() ?? "",
                update.Disposition);
            Update(current =>
            {
                var outputs = new Dictionary<string, WorkflowOutputState>(
                    current.Outputs,
                    StringComparer.Ordinal)
                {
                    [publicName] = new(
                        publicName,
                        value,
                        IsStreaming: true,
                        Done: update.Done,
                        UpdatedAt: update.ReceivedAt)
                };
                return current with { Outputs = ReadOnly(outputs) };
            });
        }

        var subscribers = StreamReceived;
        if (subscribers == null)
            return;
        foreach (Action<ExecutionStreamUpdate> subscriber in
                 subscribers.GetInvocationList())
        {
            try
            {
                subscriber(update);
            }
            catch
            {
                // A host callback must not break protocol/session processing.
            }
        }
    }

    private string? ResolvePublicName(ExecutionOutputUpdate update)
    {
        if (!string.IsNullOrWhiteSpace(update.NodeId))
        {
            return _routes.TryGetValue($"node:{update.NodeId}", out var byNode)
                ? byNode
                : null;
        }
        if (_routes.TryGetValue($"output:{update.OutputName}", out var byOutput))
            return byOutput;
        return _routes.TryGetValue($"name:{update.NodeName}", out var byName)
            ? byName
            : null;
    }

    private string? ResolvePublicName(ExecutionStreamUpdate update)
    {
        if (!string.IsNullOrWhiteSpace(update.NodeId) &&
            _routes.TryGetValue($"node:{update.NodeId}", out var byNode))
        {
            return byNode;
        }
        return !string.IsNullOrWhiteSpace(update.OutputName) &&
            _routes.TryGetValue($"output:{update.OutputName}", out var byOutput)
                ? byOutput
                : null;
    }

    private NodeToolValue Accumulate(
        string publicName,
        ExecutionOutputUpdate update)
    {
        if (!TryGetTextContent(update.Value, out var content))
        {
            return update.Value;
        }

        return AccumulateText(publicName, content, update.Disposition);
    }

    private NodeToolValue AccumulateText(
        string publicName,
        string content,
        string disposition)
    {
        lock (_gate)
        {
            if (!_chunkBuffers.TryGetValue(publicName, out var buffer))
            {
                buffer = new StringBuilder();
                _chunkBuffers[publicName] = buffer;
            }
            if (string.Equals(
                disposition,
                "replace",
                StringComparison.OrdinalIgnoreCase))
            {
                buffer.Clear();
            }
            buffer.Append(content);
            return NodeToolValue.From(buffer.ToString());
        }
    }

    private static bool IsTextStreamUpdate(ExecutionOutputUpdate update)
        => update.Value.Kind == NodeToolValueKind.String ||
            (update.Value.Kind == NodeToolValueKind.Map &&
                string.Equals(
                    update.Value.TypeDiscriminator,
                    "chunk",
                    StringComparison.OrdinalIgnoreCase) &&
                TryGetTextContent(update.Value, out _));

    private static bool TryGetTextContent(
        NodeToolValue value,
        out string content)
    {
        if (value.Kind == NodeToolValueKind.String)
        {
            content = value.AsString() ?? "";
            return true;
        }
        if (value.Kind != NodeToolValueKind.Map ||
            !string.Equals(
                value.TypeDiscriminator,
                "chunk",
                StringComparison.OrdinalIgnoreCase))
        {
            content = "";
            return false;
        }

        var map = value.AsMapOrEmpty();
        var contentType = map.TryGetValue("content_type", out var type)
            ? type.AsString()
            : null;
        var chunk = map.TryGetValue("content", out var part)
            ? part
            : NodeToolValue.From(null);
        if (!IsTextContent(contentType, chunk))
        {
            content = "";
            return false;
        }

        content = chunk.AsString() ?? "";
        return true;
    }

    private static bool IsTextContent(
        string? contentType,
        NodeToolValue content)
        => (string.IsNullOrWhiteSpace(contentType) ||
                string.Equals(
                    contentType,
                    "text",
                    StringComparison.OrdinalIgnoreCase)) &&
            content.Kind == NodeToolValueKind.String;

    private void ReconcileTerminalOutputs(
        IReadOnlyDictionary<string, NodeToolValue> terminalOutputs)
    {
        Update(current =>
        {
            var outputs = new Dictionary<string, WorkflowOutputState>(
                current.Outputs,
                StringComparer.Ordinal);
            var now = DateTimeOffset.UtcNow;
            foreach (var pair in terminalOutputs)
            {
                if (!pair.Key.StartsWith("job_result:", StringComparison.Ordinal))
                    continue;
                var publicName = pair.Key["job_result:".Length..];
                if (!_routes.ContainsKey($"output:{publicName}"))
                    continue;
                var value = UnwrapTerminalEnvelope(pair.Value);
                if (
                    outputs.TryGetValue(publicName, out var existing) &&
                    HasSameMaterializedReference(existing.Value, value))
                {
                    // output_update and the authoritative terminal snapshot
                    // commonly carry the same temp URL. Preserve UpdatedAt so
                    // hosts do not download and decode large media twice.
                    outputs[publicName] = existing with
                    {
                        IsStreaming = false,
                        Done = true
                    };
                    continue;
                }
                outputs[publicName] = new(
                    publicName,
                    value,
                    IsStreaming: false,
                    Done: true,
                    UpdatedAt: now);
            }
            return current with { Outputs = ReadOnly(outputs) };
        });
    }

    private static bool HasSameMaterializedReference(
        NodeToolValue first,
        NodeToolValue second)
    {
        if (
            first.Kind != NodeToolValueKind.Map ||
            second.Kind != NodeToolValueKind.Map)
        {
            return false;
        }

        var firstMap = first.AsMapOrEmpty();
        var secondMap = second.AsMapOrEmpty();
        if (
            !firstMap.TryGetValue("uri", out var firstUriValue) ||
            !secondMap.TryGetValue("uri", out var secondUriValue))
        {
            return false;
        }

        var firstUri = firstUriValue.AsString();
        var secondUri = secondUriValue.AsString();
        if (
            string.IsNullOrWhiteSpace(firstUri) ||
            !string.Equals(firstUri, secondUri, StringComparison.Ordinal))
        {
            return false;
        }

        var firstType = first.TypeDiscriminator;
        var secondType = second.TypeDiscriminator;
        return string.IsNullOrWhiteSpace(firstType) ||
            string.IsNullOrWhiteSpace(secondType) ||
            string.Equals(
                firstType,
                secondType,
                StringComparison.OrdinalIgnoreCase);
    }

    private static NodeToolValue UnwrapTerminalEnvelope(NodeToolValue value)
    {
        if (value.Kind != NodeToolValueKind.List)
            return value;
        var values = value.AsListOrEmpty();
        return values.Count == 1 ? values[0] : value;
    }

    private void PublishCancellationTerminal()
    {
        var state = _timeoutCancellation?.IsCancellationRequested == true
            ? WorkflowExecutionState.TimedOut
            : WorkflowExecutionState.Cancelled;
        var error = state == WorkflowExecutionState.TimedOut
            ? "Workflow execution timed out."
            : "Execution cancelled.";
        PublishTerminal(state, error);
    }

    private void PublishTerminal(WorkflowExecutionState state, string? error)
        => Update(current => current with
        {
            State = state,
            Error = error,
            CompletedAt = DateTimeOffset.UtcNow
        });

    private void Publish(WorkflowExecutionSnapshot snapshot)
    {
        lock (_gate)
        {
            if (_disposed && snapshot.State != WorkflowExecutionState.Disposed)
                return;
            _snapshot = snapshot;
        }
        Notify(snapshot);
    }

    private void Update(
        Func<WorkflowExecutionSnapshot, WorkflowExecutionSnapshot> update)
    {
        WorkflowExecutionSnapshot snapshot;
        lock (_gate)
        {
            if (_disposed)
                return;
            snapshot = update(_snapshot);
            _snapshot = snapshot;
        }
        Notify(snapshot);
    }

    private void Notify(WorkflowExecutionSnapshot snapshot)
    {
        var subscribers = SnapshotChanged;
        if (subscribers == null)
            return;
        foreach (Action<WorkflowExecutionSnapshot> subscriber in
                 subscribers.GetInvocationList())
        {
            try
            {
                subscriber(snapshot);
            }
            catch
            {
                // A host callback must not break protocol/session processing.
            }
        }
    }

    private void Attach(IExecutionSession session)
    {
        session.ProgressChanged += OnProgressChanged;
        session.NodeUpdated += OnNodeUpdated;
        session.OutputReceived += OnOutputReceived;
        session.StreamReceived += OnStreamReceived;
    }

    private void Detach(IExecutionSession session)
    {
        session.ProgressChanged -= OnProgressChanged;
        session.NodeUpdated -= OnNodeUpdated;
        session.OutputReceived -= OnOutputReceived;
        session.StreamReceived -= OnStreamReceived;
    }

    private IExecutionSession GetActiveSession()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            return _session ??
                throw new InvalidOperationException(
                    "No workflow execution is currently active.");
        }
    }

    private static Dictionary<string, string> BuildRoutes(
        IEnumerable<WorkflowOutputDescriptor> outputs)
    {
        var routes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var output in outputs)
        {
            routes[$"node:{output.NodeId}"] = output.Name;
            routes[$"output:{output.Name}"] = output.Name;
            routes[$"name:{output.Name}"] = output.Name;
        }
        return routes;
    }

    private static IReadOnlyDictionary<string, WorkflowOutputState> EmptyOutputs()
        => ReadOnly(new Dictionary<string, WorkflowOutputState>(
            StringComparer.Ordinal));

    private static IReadOnlyDictionary<string, WorkflowOutputState> ReadOnly(
        Dictionary<string, WorkflowOutputState> outputs)
        => new ReadOnlyDictionary<string, WorkflowOutputState>(outputs);

    private static bool IsActive(WorkflowExecutionState state)
        => state is
            WorkflowExecutionState.Starting or
            WorkflowExecutionState.Running or
            WorkflowExecutionState.Cancelling;

    private bool IsBusy()
        => _startReserved || IsActive(_snapshot.State);

    private async Task CancelSessionOnceAsync(
        IExecutionSession session,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_remoteCancelIssued)
                return;
            _remoteCancelIssued = true;
        }
        await session.CancelAsync(cancellationToken);
    }

    private static void ValidateInvocation(WorkflowInvocation invocation)
    {
        ArgumentNullException.ThrowIfNull(invocation);
        if (string.IsNullOrWhiteSpace(invocation.WorkflowId))
        {
            throw new ArgumentException(
                "Workflow ID must not be empty.",
                nameof(invocation));
        }
    }

    private static TaskCompletionSource<WorkflowExecutionSnapshot>
        CompletedCompletion(WorkflowExecutionSnapshot snapshot)
    {
        var completion = new TaskCompletionSource<WorkflowExecutionSnapshot>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        completion.SetResult(snapshot);
        return completion;
    }

    private void CleanupRunState()
    {
        if (_session != null)
        {
            Detach(_session);
            _session.Dispose();
            _session = null;
        }
        _runCancellation?.Dispose();
        _runCancellation = null;
        _timeoutCancellation?.Dispose();
        _timeoutCancellation = null;
        _remoteCancelIssued = false;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WorkflowExecutionController));
    }

    public async ValueTask DisposeAsync()
    {
        IExecutionSession? session;
        Task monitor;
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            _pendingInvocation = null;
            _startReserved = false;
            session = _session;
            monitor = _monitorTask;
            _runCancellation?.Cancel();
            _quiescentCompletion.TrySetResult(_snapshot);
        }

        if (session != null)
        {
            try
            {
                await CancelSessionOnceAsync(
                    session,
                    CancellationToken.None);
            }
            catch
            {
                // Best effort during disposal.
            }
        }
        try
        {
            await monitor;
        }
        catch
        {
            // Monitor failures are represented in the terminal snapshot.
        }

        lock (_gate)
        {
            CleanupRunState();
            _snapshot = _snapshot with
            {
                State = WorkflowExecutionState.Disposed,
                CompletedAt = _snapshot.CompletedAt ?? DateTimeOffset.UtcNow
            };
        }
        Publish(_snapshot);
        GC.SuppressFinalize(this);
    }

    public void Dispose()
        => DisposeAsync().AsTask().GetAwaiter().GetResult();
}
