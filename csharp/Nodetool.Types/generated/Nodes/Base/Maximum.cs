using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Maximum
{
    [Key(0)]
    public object values { get; set; } = new();

    public double Process()
    {
        return default(double);
    }
}
