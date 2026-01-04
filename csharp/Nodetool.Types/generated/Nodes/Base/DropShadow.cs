using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DropShadow
{
    [Key(0)]
    public Nodetool.Types.Core.ColorRef color { get; set; } = new Nodetool.Types.Core.ColorRef();
    [Key(1)]
    public int dx { get; set; } = 2;
    [Key(2)]
    public int dy { get; set; } = 2;
    [Key(3)]
    public double std_deviation { get; set; } = 3.0;

    public Nodetool.Types.Core.SVGElement Process()
    {
        return default(Nodetool.Types.Core.SVGElement);
    }
}
