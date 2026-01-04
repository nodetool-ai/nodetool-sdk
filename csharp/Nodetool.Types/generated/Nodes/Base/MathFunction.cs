using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class MathFunction
{
    [Key(0)]
    public object input { get; set; } = 0.0;
    [Key(1)]
    public object operation { get; set; } = @"negate";

    public object Process()
    {
        return default(object);
    }
}
