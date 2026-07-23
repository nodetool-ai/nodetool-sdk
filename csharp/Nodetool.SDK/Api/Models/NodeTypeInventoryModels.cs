using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nodetool.SDK.Api.Models;

public sealed class NodeTypeUsageExample
{
    [JsonPropertyName("node_type")]
    public string NodeType { get; set; } = string.Empty;

    [JsonPropertyName("pin")]
    public string Pin { get; set; } = string.Empty;

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = string.Empty;
}

public sealed class NodeTypeUsage
{
    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("type_name")]
    public string? TypeName { get; set; }

    [JsonPropertyName("optional")]
    public bool Optional { get; set; }

    [JsonPropertyName("type_args")]
    public List<string> TypeArguments { get; set; } = new();

    [JsonPropertyName("values")]
    public List<JsonElement> Values { get; set; } = new();

    [JsonPropertyName("values_truncated")]
    public bool ValuesTruncated { get; set; }

    [JsonPropertyName("input_uses")]
    public int InputUses { get; set; }

    [JsonPropertyName("output_uses")]
    public int OutputUses { get; set; }

    [JsonPropertyName("node_count")]
    public int NodeCount { get; set; }

    [JsonPropertyName("sources")]
    public Dictionary<string, int> Sources { get; set; } = new();

    [JsonPropertyName("examples")]
    public List<NodeTypeUsageExample> Examples { get; set; } = new();
}

public sealed class NodePackAvailabilityDiagnostic
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

public sealed class NodeTypeInventoryResponse
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("registry_revision")]
    public long RegistryRevision { get; set; }

    [JsonPropertyName("registry_ready")]
    public bool RegistryReady { get; set; }

    [JsonPropertyName("python_bridge_ready")]
    public bool PythonBridgeReady { get; set; }

    [JsonPropertyName("node_count")]
    public int NodeCount { get; set; }

    [JsonPropertyName("type_count")]
    public int TypeCount { get; set; }

    [JsonPropertyName("provenance_counts")]
    public Dictionary<string, int> ProvenanceCounts { get; set; } = new();

    [JsonPropertyName("cursor")]
    public int Cursor { get; set; }

    [JsonPropertyName("next_cursor")]
    public int? NextCursor { get; set; }

    [JsonPropertyName("types")]
    public List<NodeTypeUsage> Types { get; set; } = new();

    [JsonPropertyName("unavailable_packs")]
    public List<NodePackAvailabilityDiagnostic> UnavailablePacks { get; set; } = new();
}
