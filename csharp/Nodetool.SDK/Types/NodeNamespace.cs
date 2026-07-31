namespace Nodetool.SDK.Types;

/// <summary>
/// Resolves the hierarchical namespace used to group NodeTool node metadata.
/// </summary>
public static class NodeNamespace
{
    /// <summary>
    /// Returns the declared metadata namespace, normalized as a dot-separated
    /// path. If older metadata omits that field, the namespace is derived from
    /// the fully qualified node type.
    /// </summary>
    public static string Resolve(string? declaredNamespace, string? nodeType)
    {
        var declaredSegments = GetSegments(declaredNamespace);
        if (declaredSegments.Length > 0)
            return string.Join('.', declaredSegments);

        var nodeTypeSegments = GetSegments(nodeType);
        return nodeTypeSegments.Length > 1
            ? string.Join('.', nodeTypeSegments, 0, nodeTypeSegments.Length - 1)
            : string.Empty;
    }

    /// <summary>
    /// Splits a dot-separated namespace into its complete hierarchy, including
    /// the highest-level provider or package namespace.
    /// </summary>
    public static string[] GetSegments(string? namespacePath)
    {
        return string.IsNullOrWhiteSpace(namespacePath)
            ? []
            : namespacePath.Split(
                '.',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);
    }
}
