using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class DatetimeToDate
{
    [Key(0)]
    public Nodetool.Types.Datetime input_datetime { get; set; } = new Nodetool.Types.Datetime();

    public Nodetool.Types.Date Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.Date);
    }
}
