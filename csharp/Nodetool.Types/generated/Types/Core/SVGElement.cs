using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types.Core;

[MessagePackObject]
public class SVGElement
{
    [Key(0)]
    public Dictionary<string, string> attributes { get; set; } = new();
    [Key(1)]
    public List<Nodetool.Types.Core.SVGElement> children { get; set; }
    [Key(2)]
    public object content { get; set; } = null;
    [Key(3)]
    public string name { get; set; } = @"";
    [Key(4)]
    public object type { get; set; } = @"svg_element";
}
