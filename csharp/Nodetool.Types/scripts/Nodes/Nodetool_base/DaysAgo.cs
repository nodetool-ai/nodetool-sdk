using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class DaysAgo
{
    [Key(0)]
    public int days { get; set; } = 1;

    public Nodetool.Types.Datetime Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.Datetime);
    }
}
