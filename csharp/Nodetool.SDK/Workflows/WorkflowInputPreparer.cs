using System.Collections;
using Nodetool.SDK.Assets;
using Nodetool.SDK.Values;

namespace Nodetool.SDK.Workflows;

/// <summary>
/// Prepares workflow inputs from an authoritative portable descriptor.
/// Hosts may provide a media-value adapter for engine-specific objects before
/// the portable media pipeline handles files, bytes, references, and uploads.
/// </summary>
public sealed class WorkflowInputPreparer
{
    private readonly MediaInputPreparer _mediaInputPreparer;
    private readonly Func<
        string,
        string,
        object?,
        CancellationToken,
        ValueTask<object?>> _adaptHostMediaValue;

    public WorkflowInputPreparer(
        MediaInputPreparer mediaInputPreparer,
        Func<
            string,
            string,
            object?,
            CancellationToken,
            ValueTask<object?>>? adaptHostMediaValue = null)
    {
        _mediaInputPreparer = mediaInputPreparer
            ?? throw new ArgumentNullException(nameof(mediaInputPreparer));
        _adaptHostMediaValue = adaptHostMediaValue
            ?? ((_, _, value, _) => ValueTask.FromResult(value));
    }

    public async Task<Dictionary<string, object>> PrepareAsync(
        WorkflowDescriptor workflow,
        IReadOnlyDictionary<string, object?> inputs,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(inputs);

        var descriptors = workflow.Inputs.ToDictionary(
            input => input.Name,
            StringComparer.Ordinal);
        var prepared = new Dictionary<string, object>(
            inputs.Count,
            StringComparer.Ordinal);
        foreach (var input in inputs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            prepared[input.Key] = descriptors.TryGetValue(
                input.Key,
                out var descriptor)
                    ? await PrepareValueAsync(
                        input.Key,
                        descriptor.Type,
                        input.Value,
                        cancellationToken)
                    : Normalize(input.Value);
        }
        return prepared;
    }

    private async Task<object> PrepareValueAsync(
        string inputName,
        WorkflowTypeDescriptor type,
        object? value,
        CancellationToken cancellationToken)
    {
        var kind = type.Type.Trim().ToLowerInvariant();
        if (kind is "list" or "array" or "tuple" &&
            type.TypeArguments.FirstOrDefault() is { } elementType &&
            value is IEnumerable values and not string)
        {
            var prepared = new List<object?>();
            var index = 0;
            foreach (var item in values)
            {
                prepared.Add(await PrepareValueAsync(
                    $"{inputName}[{index++}]",
                    elementType,
                    item,
                    cancellationToken));
            }
            return prepared.ToArray();
        }

        if (IsMediaType(kind))
        {
            var adapted = await _adaptHostMediaValue(
                inputName,
                kind,
                value,
                cancellationToken);
            return await _mediaInputPreparer.PrepareAsync(
                inputName,
                kind,
                adapted,
                cancellationToken);
        }

        return Normalize(value);
    }

    private static bool IsMediaType(string type)
        => type is
            "image" or "audio" or "video" or "document" or
            "asset" or "asset_ref" or "folder" or "model_ref" or
            "model_3d" or "font";

    /// <summary>
    /// Returns whether a type or any nested type argument is media-backed.
    /// </summary>
    public static bool ContainsMedia(
        WorkflowTypeDescriptor type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var kind = type.Type.Trim().ToLowerInvariant();
        return IsMediaType(kind) ||
               type.TypeArguments.Any(ContainsMedia);
    }

    private static object Normalize(object? value)
        => NodeToolValueConverter.NormalizeForTransport(value) ?? "";
}
