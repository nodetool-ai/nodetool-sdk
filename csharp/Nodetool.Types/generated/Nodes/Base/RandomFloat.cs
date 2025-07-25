using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class RandomFloat
{
    [Key(0)]
    public double minimum { get; set; } = 0.0;
    [Key(1)]
    public double maximum { get; set; } = 1.0;

    public double Process()
    {
        return default(double);
    }
}
