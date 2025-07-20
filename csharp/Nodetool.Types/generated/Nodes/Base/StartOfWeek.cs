using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class StartOfWeek
{
    [Key(0)]
    public Nodetool.Types.Datetime input_datetime { get; set; } = new Nodetool.Types.Datetime();
    [Key(1)]
    public bool start_monday { get; set; } = true;

    public Nodetool.Types.Datetime Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.Datetime);
    }
}
