using Nodetool.SDK.Types;
using Nodetool.SDK.Workflows;

namespace Nodetool.SDK.VL.Models;

/// <summary>
/// VL projection of one portable workflow descriptor.
/// </summary>
public sealed class WorkflowDetail
{
    public WorkflowDetail(WorkflowDescriptor descriptor)
    {
        Descriptor = descriptor ??
            throw new ArgumentNullException(nameof(descriptor));
    }

    public WorkflowDescriptor Descriptor { get; }
    public string Id => Descriptor.Id;
    public string Name => Descriptor.Name;
    public string Description => Descriptor.Description;
    public string WorkflowRevision => Descriptor.Revision;
    public long? RegistryRevision => Descriptor.RegistryRevision;

    public IEnumerable<(
        string Name,
        TypeMetadata Type,
        string Description,
        object? DefaultValue,
        bool Required,
        double? Minimum,
        double? Maximum)> GetInputProperties()
    {
        foreach (var input in Descriptor.Inputs)
        {
            yield return (
                input.Name,
                ConvertType(input.Type, optional: !input.Required),
                input.Description,
                input.DefaultValue.HasValue
                    ? input.DefaultValue.Value
                    : null,
                input.Required,
                input.Minimum,
                input.Maximum);
        }
    }

    public IEnumerable<(
        string Name,
        TypeMetadata Type,
        string Description)> GetOutputProperties()
    {
        foreach (var output in Descriptor.Outputs)
        {
            yield return (
                output.Name,
                ConvertType(output.Type),
                output.Description);
        }
    }

    private static TypeMetadata ConvertType(
        WorkflowTypeDescriptor type,
        bool? optional = null)
        => new()
        {
            Type = type.Values.Count > 0
                ? "enum"
                : type.Type,
            Optional = optional ?? type.Optional,
            Values = type.Values.ToList(),
            TypeName = type.TypeName,
            TypeArgs = type.TypeArguments
                .Select(typeArgument => ConvertType(typeArgument))
                .ToList()
        };
}
