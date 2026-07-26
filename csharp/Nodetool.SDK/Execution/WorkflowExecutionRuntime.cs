using System.Diagnostics;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Assets;
using Nodetool.SDK.Workflows;

namespace Nodetool.SDK.Execution;

public sealed record WorkflowExecutionTiming(
    TimeSpan Connection,
    TimeSpan InputPreparation,
    TimeSpan RemoteExecution,
    TimeSpan Total);

public sealed record WorkflowExecutionResult(
    WorkflowExecutionSnapshot Snapshot,
    WorkflowExecutionTiming Timing);

/// <summary>
/// Owns the host-neutral lifecycle around one workflow controller: connection,
/// recursive input/media preparation, one end-to-end timeout budget,
/// cancellation, controller replacement after reconnect, and disposal.
/// </summary>
public sealed class WorkflowExecutionRuntime : IAsyncDisposable, IDisposable
{
    private readonly INodeToolExecutionConnection _connection;
    private readonly WorkflowDescriptor _workflow;
    private readonly HttpClient? _httpClient;
    private readonly Func<
        string,
        string,
        object?,
        CancellationToken,
        ValueTask<object?>>? _adaptHostMediaValue;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly object _gate = new();
    private WorkflowExecutionController? _controller;
    private INodeToolExecutionClient? _controllerClient;
    private CancellationTokenSource? _activeRun;
    private bool _running;
    private bool _disposed;
    private long _inlineMediaLimitBytes;

    public WorkflowExecutionRuntime(
        INodeToolExecutionConnection connection,
        WorkflowDescriptor workflow,
        long inlineMediaLimitBytes =
            MediaInputPreparer.DefaultInlineLimitBytes,
        HttpClient? httpClient = null,
        Func<
            string,
            string,
            object?,
            CancellationToken,
            ValueTask<object?>>? adaptHostMediaValue = null)
    {
        _connection = connection ??
            throw new ArgumentNullException(nameof(connection));
        _workflow = workflow ??
            throw new ArgumentNullException(nameof(workflow));
        _httpClient = httpClient;
        _adaptHostMediaValue = adaptHostMediaValue;
        InlineMediaLimitBytes = inlineMediaLimitBytes;
    }

    public event Action<WorkflowExecutionSnapshot>? SnapshotChanged;

    public WorkflowExecutionSnapshot Snapshot
    {
        get
        {
            lock (_gate)
                return _controller?.Snapshot ?? WorkflowExecutionSnapshot.Idle;
        }
    }

    public WorkflowExecutionTiming? LastTiming { get; private set; }

    public long InlineMediaLimitBytes
    {
        get
        {
            lock (_gate)
                return _inlineMediaLimitBytes;
        }
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            lock (_gate)
            {
                ThrowIfDisposed();
                _inlineMediaLimitBytes = value;
            }
        }
    }

    public async Task<WorkflowExecutionResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?> inputs,
        TimeSpan timeout,
        bool retainOutputs = true,
        WorkflowExecutionOptions? executionOptions = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout));

        CancellationTokenSource activeRun;
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_running)
            {
                throw new InvalidOperationException(
                    "The workflow execution runtime is already active.");
            }
            _running = true;
            activeRun = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _lifetime.Token);
            _activeRun = activeRun;
        }

        var timer = Stopwatch.StartNew();
        var phaseStarted = TimeSpan.Zero;
        var connectionDuration = TimeSpan.Zero;
        var preparationDuration = TimeSpan.Zero;
        try
        {
            WorkflowInputValidator.ValidateOrThrow(
                _workflow,
                inputs);

            using var preparationTimeout =
                new CancellationTokenSource(timeout);
            using var preparationCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(
                    activeRun.Token,
                    preparationTimeout.Token);

            INodeToolExecutionClient client;
            Dictionary<string, object> preparedInputs;
            try
            {
                client = await _connection.GetConnectedClientAsync(
                    preparationCancellation.Token);
                connectionDuration = timer.Elapsed - phaseStarted;
                phaseStarted = timer.Elapsed;

                var inputPreparation = new WorkflowInputPreparationService(
                    _connection.ApiBaseUrl,
                    _connection.AuthToken,
                    InlineMediaLimitBytes,
                    _httpClient,
                    _adaptHostMediaValue);
                preparedInputs = await inputPreparation.PrepareAsync(
                    _workflow,
                    inputs,
                    preparationCancellation.Token);
                preparationDuration = timer.Elapsed - phaseStarted;
                phaseStarted = timer.Elapsed;
            }
            catch (OperationCanceledException)
                when (preparationTimeout.IsCancellationRequested &&
                      !activeRun.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"Workflow preparation timed out after " +
                    $"{timeout.TotalSeconds:0.###} seconds.");
            }

            var remaining = timeout - timer.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                throw new TimeoutException(
                    $"Workflow preparation timed out after " +
                    $"{timeout.TotalSeconds:0.###} seconds.");
            }

            var controller = await GetOrReplaceControllerAsync(client);
            await controller.StartAsync(
                new WorkflowInvocation(
                    _workflow.Id,
                    preparedInputs.ToDictionary(
                        pair => pair.Key,
                        pair => (object?)pair.Value,
                        StringComparer.Ordinal),
                    remaining,
                    retainOutputs,
                    executionOptions),
                activeRun.Token);
            await controller.WaitForTerminalAsync();

            var timing = new WorkflowExecutionTiming(
                connectionDuration,
                preparationDuration,
                timer.Elapsed - phaseStarted,
                timer.Elapsed);
            LastTiming = timing;
            return new WorkflowExecutionResult(controller.Snapshot, timing);
        }
        finally
        {
            lock (_gate)
            {
                if (ReferenceEquals(_activeRun, activeRun))
                    _activeRun = null;
                _running = false;
            }
            activeRun.Dispose();
        }
    }

    public async Task CancelAsync(
        CancellationToken cancellationToken = default)
    {
        CancellationTokenSource? activeRun;
        WorkflowExecutionController? controller;
        lock (_gate)
        {
            ThrowIfDisposed();
            activeRun = _activeRun;
            controller = _controller;
        }

        try
        {
            activeRun?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The run reached its terminal state concurrently.
        }

        if (controller != null)
            await controller.CancelAsync(cancellationToken);
    }

    private async Task<WorkflowExecutionController>
        GetOrReplaceControllerAsync(INodeToolExecutionClient client)
    {
        WorkflowExecutionController? previous = null;
        WorkflowExecutionController controller;
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_controller != null &&
                ReferenceEquals(_controllerClient, client))
            {
                return _controller;
            }

            previous = _controller;
            if (previous != null)
                previous.SnapshotChanged -= OnSnapshotChanged;

            controller = WorkflowExecutionControllerFactory.Create(
                client,
                _workflow,
                _connection.ApiBaseUrl,
                _connection.AuthToken,
                _httpClient,
                _connection.GetSdkCapabilitiesAsync);
            controller.SnapshotChanged += OnSnapshotChanged;
            _controller = controller;
            _controllerClient = client;
        }

        if (previous != null)
            await previous.DisposeAsync();
        return controller;
    }

    private void OnSnapshotChanged(WorkflowExecutionSnapshot snapshot)
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
                // Host callbacks cannot break protocol processing.
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WorkflowExecutionRuntime));
    }

    public async ValueTask DisposeAsync()
    {
        WorkflowExecutionController? controller;
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            _lifetime.Cancel();
            try
            {
                _activeRun?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            controller = _controller;
            _controller = null;
            _controllerClient = null;
            if (controller != null)
                controller.SnapshotChanged -= OnSnapshotChanged;
        }

        if (controller != null)
            await controller.DisposeAsync();
        _lifetime.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Dispose()
        => DisposeAsync().AsTask().GetAwaiter().GetResult();
}
