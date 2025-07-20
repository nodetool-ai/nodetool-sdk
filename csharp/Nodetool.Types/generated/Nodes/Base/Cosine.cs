using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Cosine
{
    [Key(0)]
    public object angle_rad { get; set; } = 0.0;

    public double Process()
    {
        return default(double);
    }
}
