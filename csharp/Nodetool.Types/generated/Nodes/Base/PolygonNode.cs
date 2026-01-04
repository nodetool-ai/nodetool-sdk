using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class PolygonNode
{
    [Key(0)]
    public Nodetool.Types.Core.ColorRef fill { get; set; } = new Nodetool.Types.Core.ColorRef();
    [Key(1)]
    public string points { get; set; } = null;
    [Key(2)]
    public Nodetool.Types.Core.ColorRef stroke { get; set; } = new Nodetool.Types.Core.ColorRef();
    [Key(3)]
    public int stroke_width { get; set; } = 1;

    public Nodetool.Types.Core.SVGElement Process()
    {
        return default(Nodetool.Types.Core.SVGElement);
    }
}
