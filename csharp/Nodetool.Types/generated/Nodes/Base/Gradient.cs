using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Gradient
{
    [Key(0)]
    public Nodetool.Types.Core.ColorRef color1 { get; set; } = new Nodetool.Types.Core.ColorRef();
    [Key(1)]
    public Nodetool.Types.Core.ColorRef color2 { get; set; } = new Nodetool.Types.Core.ColorRef();
    [Key(2)]
    public object gradient_type { get; set; } = @"linearGradient";
    [Key(3)]
    public double x1 { get; set; } = 0;
    [Key(4)]
    public double x2 { get; set; } = 100;
    [Key(5)]
    public double y1 { get; set; } = 0;
    [Key(6)]
    public double y2 { get; set; } = 100;

    public Nodetool.Types.Core.SVGElement Process()
    {
        return default(Nodetool.Types.Core.SVGElement);
    }
}
