using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Power
{
    [Key(0)]
    public object base_ { get; set; } = 0.0;
    [Key(1)]
    public object exponent { get; set; } = 1.0;

    public object Process()
    {
        return default(object);
    }
}
