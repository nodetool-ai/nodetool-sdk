using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class RectNode
{
    [Key(0)]
    public Nodetool.Types.Core.ColorRef fill { get; set; } = new Nodetool.Types.Core.ColorRef();
    [Key(1)]
    public int height { get; set; } = 100;
    [Key(2)]
    public Nodetool.Types.Core.ColorRef stroke { get; set; } = new Nodetool.Types.Core.ColorRef();
    [Key(3)]
    public int stroke_width { get; set; } = 1;
    [Key(4)]
    public int width { get; set; } = 100;
    [Key(5)]
    public int x { get; set; } = 0;
    [Key(6)]
    public int y { get; set; } = 0;

    public Nodetool.Types.Core.SVGElement Process()
    {
        return default(Nodetool.Types.Core.SVGElement);
    }
}
