using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class StartOfMonth
{
    [Key(0)]
    public Nodetool.Types.Datetime input_datetime { get; set; } = new Nodetool.Types.Datetime();

    public Nodetool.Types.Datetime Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.Datetime);
    }
}
