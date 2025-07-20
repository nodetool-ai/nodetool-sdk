using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class ListScheduledEvents
{
    [Key(0)]
    public string user { get; set; } = "";
    [Key(1)]
    public int count { get; set; } = 20;
    [Key(2)]
    public string status { get; set; } = "active";

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
