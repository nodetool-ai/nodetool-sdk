using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ColorBalance
{
    [Key(0)]
    public Nodetool.Types.VideoRef video { get; set; } = new Nodetool.Types.VideoRef();
    [Key(1)]
    public double red_adjust { get; set; } = 1.0;
    [Key(2)]
    public double green_adjust { get; set; } = 1.0;
    [Key(3)]
    public double blue_adjust { get; set; } = 1.0;

    public Nodetool.Types.VideoRef Process()
    {
        return default(Nodetool.Types.VideoRef);
    }
}
