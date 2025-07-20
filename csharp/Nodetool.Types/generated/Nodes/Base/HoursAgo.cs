using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class HoursAgo
{
    [Key(0)]
    public int hours { get; set; } = 1;

    public Nodetool.Types.Datetime Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.Datetime);
    }
}
