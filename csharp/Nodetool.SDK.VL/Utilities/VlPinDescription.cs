using System;
using System.Collections.Generic;
using VL.Core;

namespace Nodetool.SDK.VL.Utilities;

/// <summary>
/// Pin description that can be marked as not visible by default (vvvv optional pins).
/// VL will expose these pins via the node's Configure UI.
/// </summary>
internal sealed class VlPinDescription : IVLPinDescription, IVLPinDescriptionWithVisibility
{
    public VlPinDescription(
        string name,
        Type type,
        object defaultValue,
        string summary = "",
        string remarks = "",
        bool isVisible = true,
        IReadOnlyList<string>? tags = null)
    {
        Name = name;
        Type = type;
        DefaultValue = defaultValue;
        Summary = summary;
        Remarks = remarks;
        IsVisible = isVisible;
        Tags = tags ?? Array.Empty<string>();
    }

    public string Name { get; }
    public Type Type { get; }
    public object DefaultValue { get; }

    // Not part of IVLPinDescription, but used by the editor for pin tooltips when present.
    public string Summary { get; }
    public string Remarks { get; }
    public IReadOnlyList<string> Tags { get; }

    // IVLPinDescriptionWithVisibility
    public bool IsVisible { get; }
}


