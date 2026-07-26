using System.Text.Json.Serialization;

namespace Nodetool.SDK.Api.Models;

public sealed class SdkCapabilitiesResponse
{
    [JsonPropertyName("protocol_version")]
    public string ProtocolVersion { get; set; } = string.Empty;

    [JsonPropertyName("nodetool_version")]
    public string NodetoolVersion { get; set; } = string.Empty;

    [JsonPropertyName("server_time")]
    public DateTimeOffset ServerTime { get; set; }

    [JsonPropertyName("supported_encodings")]
    public List<string> SupportedEncodings { get; set; } = new();

    [JsonPropertyName("default_encoding")]
    public string DefaultEncoding { get; set; } = string.Empty;

    [JsonPropertyName("profiles")]
    public Dictionary<string, string> Profiles { get; set; } =
        new(StringComparer.Ordinal);

    [JsonPropertyName("registry_revision")]
    public long RegistryRevision { get; set; }

    [JsonPropertyName("python_bridge")]
    public string PythonBridge { get; set; } = string.Empty;

    [JsonPropertyName("auth_modes")]
    public List<string> AuthModes { get; set; } = new();

    [JsonPropertyName("asset_uri_schemes")]
    public List<string> AssetUriSchemes { get; set; } = new();

    [JsonPropertyName("execution_options")]
    public SdkExecutionOptionsCapabilities? ExecutionOptions { get; set; }

    [JsonPropertyName("limits")]
    public SdkCapabilityLimits Limits { get; set; } = new();
}

public sealed class SdkExecutionOptionsCapabilities
{
    [JsonPropertyName("persistence")]
    public List<string> Persistence { get; set; } = new();

    [JsonPropertyName("event_detail")]
    public List<string> EventDetail { get; set; } = new();

    [JsonPropertyName("asset_persistence")]
    public List<string> AssetPersistence { get; set; } = new();

    [JsonPropertyName("defaults")]
    public SdkExecutionOptionDefaults Defaults { get; set; } = new();
}

public sealed class SdkExecutionOptionDefaults
{
    [JsonPropertyName("persistence")]
    public string Persistence { get; set; } = string.Empty;

    [JsonPropertyName("event_detail")]
    public string EventDetail { get; set; } = string.Empty;

    [JsonPropertyName("asset_persistence")]
    public string AssetPersistence { get; set; } = string.Empty;
}

public sealed class SdkCapabilityLimits
{
    [JsonPropertyName("max_rpc_batch")]
    public int MaxRpcBatch { get; set; }

    [JsonPropertyName("max_inline_bytes")]
    public long MaxInlineBytes { get; set; }

    [JsonPropertyName("max_upload_bytes")]
    public long MaxUploadBytes { get; set; }

    [JsonPropertyName("max_queued_jobs")]
    public int MaxQueuedJobs { get; set; }

    [JsonPropertyName("max_job_event_replay")]
    public int MaxJobEventReplay { get; set; }

    [JsonPropertyName("request_timeout_seconds")]
    public double RequestTimeoutSeconds { get; set; }
}
