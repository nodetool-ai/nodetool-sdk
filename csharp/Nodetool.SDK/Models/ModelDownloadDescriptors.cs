using Nodetool.SDK.Api.Models;

namespace Nodetool.SDK.Models;

public sealed record ModelDownloadState(
    string OperationId,
    string Scope,
    string RepositoryId,
    string? Path,
    string ModelType,
    string Status,
    long DownloadedBytes,
    long TotalBytes,
    int DownloadedFiles,
    IReadOnlyList<string> CurrentFiles,
    int TotalFiles,
    string? Error,
    DateTimeOffset StartedAt,
    DateTimeOffset UpdatedAt)
{
    public bool IsTerminal => SdkModelDownloadStatuses.IsTerminal(Status);

    public double? Progress => TotalBytes > 0
        ? Math.Clamp((double)DownloadedBytes / TotalBytes, 0d, 1d)
        : null;
}

public sealed record ModelDownloadSnapshot(
    string Scope,
    IReadOnlyList<ModelDownloadState> Downloads,
    DateTimeOffset RefreshedAt)
{
    public static ModelDownloadSnapshot Empty(string scope)
        => new(scope, [], DateTimeOffset.MinValue);
}
