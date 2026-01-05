using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Peek
{
    [Key(0)]
    public Nodetool.Types.Core.Collection collection { get; set; } = new Nodetool.Types.Core.Collection();
    [Key(1)]
    public int limit { get; set; } = 100;

    public object Process()
    {
        return default(object);
    }
}
