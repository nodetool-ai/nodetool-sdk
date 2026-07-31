using System.Text.Json.Serialization;

namespace Nodetool.SDK.Api.Models;

public static class SdkModelDownloadStatuses
{
    public const string Start = "start";
    public const string Progress = "progress";
    public const string Completed = "completed";
    public const string Error = "error";
    public const string Cancelled = "cancelled";

    public static bool IsTerminal(string status)
        => status is Completed or Error or Cancelled;
}

public sealed record SdkModelDownloadStartRequest(
    [property: JsonPropertyName("repo_id")] string RepositoryId,
    [property: JsonPropertyName("model_type")] string ModelType,
    [property: JsonPropertyName("path")] string? Path = null,
    [property: JsonPropertyName("scope")] string Scope = SdkModelScopes.Local,
    [property: JsonPropertyName("allow_patterns")] IReadOnlyList<string>? AllowPatterns = null,
    [property: JsonPropertyName("ignore_patterns")] IReadOnlyList<string>? IgnorePatterns = null);

public sealed record SdkModelDownloadCancelRequest(
    [property: JsonPropertyName("operation_id")] string OperationId);

public sealed record SdkModelDownloadQuery(
    string Scope = SdkModelScopes.Local,
    string? OperationId = null);

public sealed class SdkModelDownloadStateResponse
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("operation_id")]
    public string OperationId { get; set; } = string.Empty;

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    [JsonPropertyName("repo_id")]
    public string RepositoryId { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("model_type")]
    public string ModelType { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("downloaded_bytes")]
    public long DownloadedBytes { get; set; }

    [JsonPropertyName("total_bytes")]
    public long TotalBytes { get; set; }

    [JsonPropertyName("downloaded_files")]
    public int DownloadedFiles { get; set; }

    [JsonPropertyName("current_files")]
    public List<string> CurrentFiles { get; set; } = new();

    [JsonPropertyName("total_files")]
    public int TotalFiles { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("started_at")]
    public DateTimeOffset StartedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class SdkModelDownloadSnapshotResponse
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("downloads")]
    public List<SdkModelDownloadStateResponse> Downloads { get; set; } = new();
}
