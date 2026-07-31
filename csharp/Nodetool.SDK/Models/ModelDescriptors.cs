using System.Text.Json;
using Nodetool.SDK.Values;

namespace Nodetool.SDK.Models;

public sealed record ModelDescriptor(
    string Key,
    string DisplayName,
    string Compatibility,
    string Availability,
    bool Recommended,
    string Scope,
    string? Provider,
    string Id,
    string? RepositoryId,
    string? Path,
    IReadOnlyList<string> SupportedTasks,
    long? SizeOnDisk,
    JsonElement WireValue)
{
    public bool IsReady =>
        Availability is Api.Models.SdkModelAvailability.ReadyLocal or
            Api.Models.SdkModelAvailability.ReadyRemote;

    public ModelSelection Select()
        => ModelSelection.FromDescriptor(this);
}

public sealed record ModelSelection(
    string Key,
    string DisplayName,
    string Compatibility,
    JsonElement WireValue)
{
    public static ModelSelection FromDescriptor(ModelDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (descriptor.WireValue.ValueKind != JsonValueKind.Object ||
            !descriptor.WireValue.TryGetProperty("type", out var type) ||
            !string.Equals(
                type.GetString(),
                descriptor.Compatibility,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Model '{descriptor.Key}' has a wire value incompatible with '{descriptor.Compatibility}'.");
        }

        return new ModelSelection(
            descriptor.Key,
            descriptor.DisplayName,
            descriptor.Compatibility,
            descriptor.WireValue.Clone());
    }

    public object ToInputValue()
        => NodeToolValueConverter.Convert(WireValue, typeof(object))
           ?? throw new InvalidDataException(
               $"Model '{Key}' produced an empty input value.");
}

public sealed record ModelCatalogSnapshot(
    string Revision,
    string Scope,
    IReadOnlyList<ModelDescriptor> Models,
    DateTimeOffset? LastSuccessfulRefreshUtc,
    bool IsStale,
    string? LastError)
{
    public static ModelCatalogSnapshot Empty { get; } = new(
        string.Empty,
        Api.Models.SdkModelScopes.Local,
        Array.Empty<ModelDescriptor>(),
        null,
        false,
        null);
}
