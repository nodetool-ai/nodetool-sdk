using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Diagnostics;

namespace Nodetool.SDK.Workflows;

/// <summary>
/// Host-neutral workflow catalog with revision-aware interface caching and
/// last-known-good refresh behavior.
/// </summary>
public sealed class WorkflowCatalog : IWorkflowCatalog, IDisposable
{
    private sealed record CacheKey(string Scope, string WorkflowId);
    private sealed record CachedDescriptor(
        string Revision,
        long RegistryRevision,
        string InterfaceEtag,
        WorkflowDescriptor Descriptor);

    private static readonly ConcurrentDictionary<CacheKey, CachedDescriptor>
        SharedCache = new();

    private readonly IWorkflowDiscoveryClient _client;
    private readonly ILogger<WorkflowCatalog> _logger;
    private readonly string _scope;
    private readonly TimeSpan _cacheDuration;
    private readonly int _batchSize;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private DateTimeOffset _expiresUtc = DateTimeOffset.MinValue;
    private WorkflowCatalogSnapshot _snapshot = WorkflowCatalogSnapshot.Empty;
    private bool _disposed;

    public WorkflowCatalogSnapshot Snapshot => Volatile.Read(ref _snapshot);

    public WorkflowCatalog(
        IWorkflowDiscoveryClient client,
        string scope,
        TimeSpan? cacheDuration = null,
        int batchSize = 100,
        ILogger<WorkflowCatalog>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize));

        _client = client;
        _scope = scope;
        _cacheDuration = cacheDuration ?? TimeSpan.FromMinutes(5);
        _batchSize = batchSize;
        _logger = logger ?? NullLogger<WorkflowCatalog>.Instance;
    }

    public async Task<WorkflowCatalogSnapshot> RefreshAsync(
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!force && HasFreshSnapshot())
            return Snapshot;

        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!force && HasFreshSnapshot())
                return Snapshot;

            return await RefreshCoreAsync(force, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public WorkflowDescriptor? GetById(string workflowId)
        => Snapshot.Workflows.FirstOrDefault(
            workflow => string.Equals(
                workflow.Id,
                workflowId,
                StringComparison.Ordinal));

    public void Clear()
    {
        Volatile.Write(ref _snapshot, WorkflowCatalogSnapshot.Empty);
        _expiresUtc = DateTimeOffset.MinValue;
    }

    private bool HasFreshSnapshot()
        => Snapshot.LastSuccessfulRefreshUtc.HasValue &&
           DateTimeOffset.UtcNow < _expiresUtc;

    private async Task<WorkflowCatalogSnapshot> RefreshCoreAsync(
        bool force,
        CancellationToken cancellationToken)
    {
        var previous = Snapshot;
        try
        {
            var summaries = await _client
                .GetWorkflowSummariesAsync(cancellationToken)
                .ConfigureAwait(false);
            var descriptors = new Dictionary<string, WorkflowDescriptor>(
                StringComparer.Ordinal);
            var pending = new List<WorkflowSummaryResponse>();
            var cacheHits = 0;
            var skipped = 0;

            foreach (var summary in summaries)
            {
                if (!force && TryGetCached(summary, out var cached))
                {
                    descriptors[summary.Id] = cached;
                    cacheHits++;
                }
                else
                {
                    pending.Add(summary);
                }
            }

            foreach (var batch in pending.Chunk(_batchSize))
            {
                var summariesById = batch.ToDictionary(
                    summary => summary.Id,
                    StringComparer.Ordinal);
                var response = await _client.GetWorkflowInterfacesAsync(
                    summariesById.Keys.ToArray(),
                    cancellationToken).ConfigureAwait(false);
                var resolvedIds = new HashSet<string>(StringComparer.Ordinal);

                foreach (var workflowInterface in response.Interfaces)
                {
                    if (!summariesById.TryGetValue(
                            workflowInterface.WorkflowId,
                            out var summary))
                    {
                        skipped++;
                        continue;
                    }
                    resolvedIds.Add(workflowInterface.WorkflowId);

                    if (workflowInterface.Diagnostics.Any(IsError))
                    {
                        skipped++;
                        PreservePrevious(previous, summary.Id, descriptors);
                        continue;
                    }

                    var descriptor = CreateDescriptor(summary, workflowInterface);
                    descriptors[summary.Id] = descriptor;
                    StoreCached(summary, workflowInterface, descriptor);
                }

                foreach (var error in response.Errors)
                {
                    resolvedIds.Add(error.WorkflowId);
                    skipped++;
                    PreservePrevious(previous, error.WorkflowId, descriptors);
                    _logger.LogWarning(
                        "Workflow interface {WorkflowId} was skipped ({Code}): {Message}",
                        error.WorkflowId,
                        error.Code,
                        error.Message);
                }

                foreach (var missingId in summariesById.Keys.Except(resolvedIds))
                {
                    skipped++;
                    PreservePrevious(previous, missingId, descriptors);
                    _logger.LogWarning(
                        "Workflow interface {WorkflowId} was missing from the batch response",
                        missingId);
                }
            }

            var activeIds = summaries
                .Select(summary => summary.Id)
                .ToHashSet(StringComparer.Ordinal);
            PruneSharedCache(activeIds);

            var ordered = summaries
                .Where(summary => descriptors.ContainsKey(summary.Id))
                .Select(summary => descriptors[summary.Id])
                .ToArray();
            var refreshed = new WorkflowCatalogSnapshot(
                ordered,
                DateTimeOffset.UtcNow,
                false,
                null,
                cacheHits,
                skipped);
            Volatile.Write(ref _snapshot, refreshed);
            _expiresUtc = DateTimeOffset.UtcNow + _cacheDuration;
            return refreshed;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var safeError =
                NodeToolDiagnosticRedactor.RedactText(ex.Message);
            var stale = previous with
            {
                IsStale = previous.LastSuccessfulRefreshUtc.HasValue,
                LastError = safeError
            };
            Volatile.Write(ref _snapshot, stale);
            _logger.LogWarning(
                "Workflow catalog refresh failed: {Error}",
                safeError);
            return stale;
        }
    }

    private bool TryGetCached(
        WorkflowSummaryResponse summary,
        out WorkflowDescriptor descriptor)
    {
        descriptor = null!;
        if (string.IsNullOrWhiteSpace(summary.Revision) ||
            !summary.RegistryRevision.HasValue ||
            !SharedCache.TryGetValue(
                new CacheKey(_scope, summary.Id),
                out var cached) ||
            !string.Equals(
                cached.Revision,
                summary.Revision,
                StringComparison.Ordinal) ||
            cached.RegistryRevision != summary.RegistryRevision.Value)
        {
            return false;
        }

        descriptor = cached.Descriptor;
        return true;
    }

    private void StoreCached(
        WorkflowSummaryResponse summary,
        WorkflowInterfaceResponse workflowInterface,
        WorkflowDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(summary.Revision) ||
            !summary.RegistryRevision.HasValue ||
            string.IsNullOrWhiteSpace(workflowInterface.Etag))
        {
            return;
        }

        SharedCache[new CacheKey(_scope, summary.Id)] = new CachedDescriptor(
            summary.Revision,
            summary.RegistryRevision.Value,
            workflowInterface.Etag,
            descriptor);
    }

    private static void PreservePrevious(
        WorkflowCatalogSnapshot previous,
        string workflowId,
        IDictionary<string, WorkflowDescriptor> descriptors)
    {
        var descriptor = previous.Workflows.FirstOrDefault(
            workflow => string.Equals(
                workflow.Id,
                workflowId,
                StringComparison.Ordinal));
        if (descriptor is not null)
            descriptors[workflowId] = descriptor;
    }

    private void PruneSharedCache(IReadOnlySet<string> activeIds)
    {
        foreach (var key in SharedCache.Keys)
        {
            if (string.Equals(key.Scope, _scope, StringComparison.Ordinal) &&
                !activeIds.Contains(key.WorkflowId))
            {
                SharedCache.TryRemove(key, out _);
            }
        }
    }

    private static bool IsError(WorkflowInterfaceDiagnostic diagnostic)
        => string.Equals(
            diagnostic.Severity,
            "error",
            StringComparison.OrdinalIgnoreCase);

    private static WorkflowDescriptor CreateDescriptor(
        WorkflowSummaryResponse summary,
        WorkflowInterfaceResponse workflowInterface)
        => new(
            summary.Id,
            summary.Name,
            summary.Description,
            summary.Revision,
            summary.RegistryRevision,
            summary.RunMode,
            workflowInterface.Version,
            workflowInterface.Etag,
            workflowInterface.Source,
            workflowInterface.Inputs.Select(ConvertInput).ToArray(),
            workflowInterface.Outputs.Select(ConvertOutput).ToArray(),
            workflowInterface.Diagnostics.Select(ConvertDiagnostic).ToArray());

    private static WorkflowInputDescriptor ConvertInput(
        WorkflowInterfaceInput input)
        => new(
            input.NodeId,
            input.Name,
            input.Description,
            ConvertType(input.Type, optional: !input.Required),
            input.Required,
            input.Default.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
                ? null
                : input.Default.Clone(),
            input.Min,
            input.Max);

    private static WorkflowOutputDescriptor ConvertOutput(
        WorkflowInterfaceOutput output)
        => new(
            output.NodeId,
            output.Name,
            output.Description,
            ConvertType(output.Type),
            output.Stream);

    private static WorkflowDiagnosticDescriptor ConvertDiagnostic(
        WorkflowInterfaceDiagnostic diagnostic)
        => new(
            diagnostic.Severity,
            diagnostic.Code,
            diagnostic.Message,
            diagnostic.NodeId,
            diagnostic.PinName);

    private static WorkflowTypeDescriptor ConvertType(
        NodeTypeDefinition type,
        bool? optional = null)
        => new(
            type.Type,
            optional ?? type.Optional,
            type.TypeName,
            type.Values?.ToArray() ?? Array.Empty<object>(),
            type.TypeArgs?.Select(argument => ConvertType(argument)).ToArray()
                ?? Array.Empty<WorkflowTypeDescriptor>());

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _refreshLock.Dispose();
    }
}
