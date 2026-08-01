using Nodetool.SDK.Api.Models;
using Nodetool.SDK.VL.Utilities;

namespace Nodetool.SDK.VL.Streaming;

internal sealed record NodeAudioSourcePin(
    NodeOutput Output,
    string PinName);

internal static class NodeAudioSourcePins
{
    public static IReadOnlyList<NodeAudioSourcePin> Create(
        NodeMetadataResponse metadata,
        IEnumerable<string> reservedPinNames)
    {
        var reserved = new HashSet<string>(
            reservedPinNames,
            StringComparer.Ordinal);
        var result = new List<NodeAudioSourcePin>();

        foreach (var output in metadata.Outputs)
        {
            if ((!output.Stream && !metadata.IsStreamingOutput) ||
                !VlTypeMapping.IsAudioReference(output.Type))
            {
                continue;
            }

            var baseName = string.Equals(
                output.Name,
                "audio",
                StringComparison.OrdinalIgnoreCase)
                    ? "Audio Source"
                    : $"{output.Name} Audio Source";
            var pinName = baseName;
            var suffix = 2;
            while (!reserved.Add(pinName))
                pinName = $"{baseName} {suffix++}";

            result.Add(new NodeAudioSourcePin(output, pinName));
        }

        return result;
    }
}
