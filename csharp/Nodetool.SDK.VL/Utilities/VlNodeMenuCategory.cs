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
            // Category casing is part of a dynamic node's serialized identity
            // in a .vl patch. Preserve NodeTool's canonical namespace instead
            // of prettifying it and invalidating existing node references.
            : $"{Root}.{nodeNamespace}";
    }
}
