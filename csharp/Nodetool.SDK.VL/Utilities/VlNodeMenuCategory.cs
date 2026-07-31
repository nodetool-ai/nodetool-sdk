using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Types;

namespace Nodetool.SDK.VL.Utilities;

internal static class VlNodeMenuCategory
{
    internal const string Root = "Nodetool Nodes";

    public static string For(NodeMetadataResponse nodeMetadata)
    {
        ArgumentNullException.ThrowIfNull(nodeMetadata);

        var nodeNamespace = NodeNamespace.Resolve(
            nodeMetadata.Namespace,
            nodeMetadata.NodeType);

        return string.IsNullOrEmpty(nodeNamespace)
            ? $"{Root}.General"
            : $"{Root}.{FormatNamespace(nodeNamespace)}";
    }

    private static string FormatNamespace(string nodeNamespace)
        => string.Join(
            '.',
            NodeNamespace.GetSegments(nodeNamespace)
                .Select(CapitalizeSegment));

    private static string CapitalizeSegment(string segment)
        => segment.Length == 0
            ? segment
            : char.ToUpperInvariant(segment[0]) + segment[1..];
}
