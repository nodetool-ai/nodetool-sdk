namespace Nodetool.SDK.VL.Utilities;

/// <summary>
/// Coalesces asynchronous AppHost discovery/factory milestones into one
/// concise readiness message. Repeated identical component errors are
/// suppressed until that component succeeds or the host is reset.
/// </summary>
internal sealed class VlReadinessState
{
    private readonly object _gate = new();
    private readonly Action<string> _writeInfo;
    private readonly Action<string> _writeError;
    private readonly Dictionary<string, string> _lastErrors =
        new(StringComparer.Ordinal);
    private bool _registered;
    private bool _nodesDiscovered;
    private bool _workflowsDiscovered;
    private bool _nodesFactoryResolved;
    private bool _workflowsFactoryResolved;
    private bool _readyEmitted;
    private int _nodeCount;
    private int _workflowCount;
    private string _workflowTransport = "unknown";

    public VlReadinessState(
        Action<string> writeInfo,
        Action<string> writeError)
    {
        _writeInfo = writeInfo ??
            throw new ArgumentNullException(nameof(writeInfo));
        _writeError = writeError ??
            throw new ArgumentNullException(nameof(writeError));
    }

    public void Reset()
    {
        lock (_gate)
        {
            _registered = false;
            _nodesDiscovered = false;
            _workflowsDiscovered = false;
            _nodesFactoryResolved = false;
            _workflowsFactoryResolved = false;
            _readyEmitted = false;
            _nodeCount = 0;
            _workflowCount = 0;
            _workflowTransport = "unknown";
            _lastErrors.Clear();
        }
    }

    public void MarkRegistered()
    {
        lock (_gate)
        {
            _registered = true;
            TryEmitReady();
        }
    }

    public void MarkNodeDiscovery(int count)
    {
        lock (_gate)
        {
            _nodesDiscovered = true;
            _nodeCount = count;
            _lastErrors.Remove("node discovery");
            TryEmitReady();
        }
    }

    public void MarkWorkflowDiscovery(int count, string transport)
    {
        lock (_gate)
        {
            _workflowsDiscovered = true;
            _workflowCount = count;
            _workflowTransport = string.IsNullOrWhiteSpace(transport)
                ? "unknown"
                : transport;
            _lastErrors.Remove("workflow discovery");
            TryEmitReady();
        }
    }

    public void MarkNodeFactoryResolved()
    {
        lock (_gate)
        {
            _nodesFactoryResolved = true;
            _lastErrors.Remove("node factory");
            TryEmitReady();
        }
    }

    public void MarkWorkflowFactoryResolved()
    {
        lock (_gate)
        {
            _workflowsFactoryResolved = true;
            _lastErrors.Remove("workflow factory");
            TryEmitReady();
        }
    }

    public void ReportError(string component, string message)
    {
        lock (_gate)
        {
            var safeComponent = string.IsNullOrWhiteSpace(component)
                ? "runtime"
                : component.Trim();
            var safeMessage = string.IsNullOrWhiteSpace(message)
                ? "Unknown error."
                : message.Trim();
            if (_lastErrors.TryGetValue(safeComponent, out var previous) &&
                string.Equals(previous, safeMessage, StringComparison.Ordinal))
            {
                return;
            }

            _lastErrors[safeComponent] = safeMessage;
            _writeError($"{safeComponent}: {safeMessage}");
        }
    }

    private void TryEmitReady()
    {
        if (_readyEmitted ||
            !_registered ||
            !_nodesDiscovered ||
            !_workflowsDiscovered ||
            !_nodesFactoryResolved ||
            !_workflowsFactoryResolved)
        {
            return;
        }

        _readyEmitted = true;
        _writeInfo(
            $"ready: connection and discovery resolved; {_nodeCount} nodes, " +
            $"{_workflowCount} workflows via {_workflowTransport}; factories ready.");
    }
}

internal static class VlReadinessLog
{
    private static readonly VlReadinessState State =
        new(VlLog.Info, VlLog.Error);

    public static void Reset() => State.Reset();
    public static void MarkRegistered() => State.MarkRegistered();
    public static void MarkNodeDiscovery(int count)
        => State.MarkNodeDiscovery(count);
    public static void MarkWorkflowDiscovery(int count, string transport)
        => State.MarkWorkflowDiscovery(count, transport);
    public static void MarkNodeFactoryResolved()
        => State.MarkNodeFactoryResolved();
    public static void MarkWorkflowFactoryResolved()
        => State.MarkWorkflowFactoryResolved();
    public static void ReportError(string component, string message)
        => State.ReportError(component, message);
}
