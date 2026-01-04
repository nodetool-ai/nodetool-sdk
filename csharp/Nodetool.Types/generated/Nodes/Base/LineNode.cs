using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class LineNode
{
    [Key(0)]
    public Nodetool.Types.Core.ColorRef stroke { get; set; } = new Nodetool.Types.Core.ColorRef();
    [Key(1)]
    public int stroke_width { get; set; } = 1;
    [Key(2)]
    public int x1 { get; set; } = 0;
    [Key(3)]
    public int x2 { get; set; } = 100;
    [Key(4)]
    public int y1 { get; set; } = 0;
    [Key(5)]
    public int y2 { get; set; } = 100;

    public Nodetool.Types.Core.SVGElement Process()
    {
        return default(Nodetool.Types.Core.SVGElement);
    }
}
