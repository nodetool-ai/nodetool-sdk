using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class Integer
{
    [Key(0)]
    public int value { get; set; } = 0;

    public int Process()
    {
        // Implementation would be generated based on node logic
        return default(int);
    }
}
