using MessagePack;
using System.Collections.Generic;

namespace Nodetool.Types;

[MessagePackObject]
public class SVGElement
{
    [Key(0)]
    public object type { get; set; } = "svg_element";
    [Key(1)]
    public string name { get; set; } = "";
    [Key(2)]
    public Dictionary<string, string> attributes { get; set; } = new Dictionary<string, object>();
    [Key(3)]
    public object content { get; set; } = null;
    [Key(4)]
    public List<Nodetool.Types.SVGElement> children { get; set; }
}
