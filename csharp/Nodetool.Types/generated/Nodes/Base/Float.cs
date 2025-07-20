using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Float
{
    [Key(0)]
    public double value { get; set; } = 0.0;

    public double Process()
    {
        return default(double);
    }
}
