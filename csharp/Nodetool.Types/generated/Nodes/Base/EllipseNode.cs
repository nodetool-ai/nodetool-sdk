using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class EllipseNode
{
    [Key(0)]
    public int cx { get; set; } = 0;
    [Key(1)]
    public int cy { get; set; } = 0;
    [Key(2)]
    public Nodetool.Types.Core.ColorRef fill { get; set; } = new Nodetool.Types.Core.ColorRef();
    [Key(3)]
    public int rx { get; set; } = 100;
    [Key(4)]
    public int ry { get; set; } = 50;
    [Key(5)]
    public Nodetool.Types.Core.ColorRef stroke { get; set; } = new Nodetool.Types.Core.ColorRef();
    [Key(6)]
    public int stroke_width { get; set; } = 1;

    public Nodetool.Types.Core.SVGElement Process()
    {
        return default(Nodetool.Types.Core.SVGElement);
    }
}
