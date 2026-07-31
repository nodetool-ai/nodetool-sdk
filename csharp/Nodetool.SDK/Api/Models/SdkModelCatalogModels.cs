using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nodetool.SDK.Api.Models;

public static class SdkModelAvailability
{
    public const string ReadyLocal = "ready_local";
    public const string ReadyRemote = "ready_remote";
    public const string Downloadable = "downloadable";
    public const string Downloading = "downloading";
    public const string Unavailable = "unavailable";
}

public static class SdkModelScopes
{
    public const string Local = "local";
    public const string Worker = "worker";
}

public sealed record SdkModelCatalogQuery(
    string? Compatibility = null,
    string? Availability = null,
    string? Provider = null,
    string Scope = SdkModelScopes.Local,
    string? Cursor = null,
    int Limit = 200);

public sealed class SdkModelCatalogEntryResponse
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("compatibility")]
    public string Compatibility { get; set; } = string.Empty;

    [JsonPropertyName("availability")]
    public string Availability { get; set; } = string.Empty;

    [JsonPropertyName("recommended")]
    public bool Recommended { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    [JsonPropertyName("provider")]
    public string? Provider { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("repo_id")]
    public string? RepositoryId { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("supported_tasks")]
    public List<string> SupportedTasks { get; set; } = new();

    [JsonPropertyName("size_on_disk")]
    public long? SizeOnDisk { get; set; }

    [JsonPropertyName("wire_value")]
    public JsonElement WireValue { get; set; }
}

public sealed class SdkModelCatalogResponse
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("catalog_revision")]
    public string CatalogRevision { get; set; } = string.Empty;

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    [JsonPropertyName("entries")]
    public List<SdkModelCatalogEntryResponse> Entries { get; set; } = new();

    [JsonPropertyName("next_cursor")]
    public string? NextCursor { get; set; }
}
