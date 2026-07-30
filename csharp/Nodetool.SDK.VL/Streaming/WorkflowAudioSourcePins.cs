using Nodetool.SDK.Workflows;

namespace Nodetool.SDK.VL.Streaming;

internal sealed record WorkflowAudioSourcePin(
    WorkflowOutputDescriptor Output,
    string PinName);

internal static class WorkflowAudioSourcePins
{
    public static IReadOnlyList<WorkflowAudioSourcePin> Create(
        WorkflowDescriptor descriptor,
        IEnumerable<string> reservedPinNames)
    {
        var reserved = new HashSet<string>(
            reservedPinNames,
            StringComparer.Ordinal);
        var result = new List<WorkflowAudioSourcePin>();

        foreach (var output in descriptor.Outputs)
        {
            if (!output.Stream ||
                !string.Equals(
                    output.StreamKind,
                    "audio",
                    StringComparison.Ordinal))
                continue;

            var baseName = $"{output.Name} Audio Source";
            var pinName = baseName;
            var suffix = 2;
            while (!reserved.Add(pinName))
                pinName = $"{baseName} {suffix++}";

            result.Add(new WorkflowAudioSourcePin(output, pinName));
        }

        return result;
    }
}
