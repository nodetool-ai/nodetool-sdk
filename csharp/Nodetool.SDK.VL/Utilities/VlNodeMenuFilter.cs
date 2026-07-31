using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Types;

namespace Nodetool.SDK.VL.Utilities;

internal enum VlNodeMenuExclusion
{
    None,
    Hidden,
    Deprecated,
    WorkflowInfrastructure,
    NativeVvvvEquivalent
}

internal static class VlNodeMenuFilter
{
    private static readonly string[] WorkflowInfrastructureNamespaces =
    [
        "nodetool.fake",
        "nodetool.input",
        "nodetool.output",
        "nodetool.triggers",
        "nodetool.variable",
        "nodetool.workflows"
    ];

    private static readonly string[] NativeVvvvEquivalentNamespaces =
    [
        "nodetool.compare",
        "nodetool.constant",
        "nodetool.control",
        "nodetool.list"
    ];

    public static bool ShouldPublish(
        NodeMetadataResponse nodeMetadata,
        bool showAllNodes,
        out VlNodeMenuExclusion exclusion)
    {
        ArgumentNullException.ThrowIfNull(nodeMetadata);

        if (showAllNodes)
        {
            exclusion = VlNodeMenuExclusion.None;
            return true;
        }

        if (nodeMetadata.Hidden)
        {
            exclusion = VlNodeMenuExclusion.Hidden;
            return false;
        }

        if (nodeMetadata.Deprecated)
        {
            exclusion = VlNodeMenuExclusion.Deprecated;
            return false;
        }

        var nodeNamespace = NodeNamespace.Resolve(
            nodeMetadata.Namespace,
            nodeMetadata.NodeType);

        if (MatchesAnyNamespace(
            nodeNamespace,
            WorkflowInfrastructureNamespaces))
        {
            exclusion = VlNodeMenuExclusion.WorkflowInfrastructure;
            return false;
        }

        if (MatchesAnyNamespace(
            nodeNamespace,
            NativeVvvvEquivalentNamespaces))
        {
            exclusion = VlNodeMenuExclusion.NativeVvvvEquivalent;
            return false;
        }

        exclusion = VlNodeMenuExclusion.None;
        return true;
    }

    private static bool MatchesAnyNamespace(
        string nodeNamespace,
        IEnumerable<string> namespacePrefixes)
    {
        return namespacePrefixes.Any(prefix =>
            string.Equals(
                nodeNamespace,
                prefix,
                StringComparison.Ordinal) ||
            nodeNamespace.StartsWith(
                $"{prefix}.",
                StringComparison.Ordinal));
    }
}
