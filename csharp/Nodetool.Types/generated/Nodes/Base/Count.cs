using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Count
{
    [Key(0)]
    public Nodetool.Types.Core.Collection collection { get; set; } = new Nodetool.Types.Core.Collection();

    public int Process()
    {
        return default(int);
    }
}
