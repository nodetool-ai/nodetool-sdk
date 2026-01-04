using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Text
{
    [Key(0)]
    public Nodetool.Types.Core.ColorRef fill { get; set; } = new Nodetool.Types.Core.ColorRef();
    [Key(1)]
    public string font_family { get; set; } = @"Arial";
    [Key(2)]
    public int font_size { get; set; } = 16;
    [Key(3)]
    public string text { get; set; } = @"";
    [Key(4)]
    public object text_anchor { get; set; } = @"start";
    [Key(5)]
    public int x { get; set; } = 0;
    [Key(6)]
    public int y { get; set; } = 0;

    public Nodetool.Types.Core.SVGElement Process()
    {
        return default(Nodetool.Types.Core.SVGElement);
    }
}
