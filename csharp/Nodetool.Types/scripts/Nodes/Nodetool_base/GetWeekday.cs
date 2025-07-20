using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class GetWeekday
{
    [Key(0)]
    public Nodetool.Types.Datetime input_datetime { get; set; } = new Nodetool.Types.Datetime();
    [Key(1)]
    public bool as_name { get; set; } = true;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
