using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Stack
{
    [Key(0)]
    public object arrays { get; set; } = new();
    [Key(1)]
    public int axis { get; set; } = 0;

    public Nodetool.Types.Core.NPArray Process()
    {
        return default(Nodetool.Types.Core.NPArray);
    }
}
