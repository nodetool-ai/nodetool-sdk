using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_chroma;

[MessagePackObject]
public class Peek
{
    [Key(0)]
    public Nodetool.Types.Collection collection { get; set; } = new Nodetool.Types.Collection();
    [Key(1)]
    public int limit { get; set; } = 100;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
