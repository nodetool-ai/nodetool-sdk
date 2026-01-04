using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DatetimeToDate
{
    [Key(0)]
    public Nodetool.Types.Core.Datetime input_datetime { get; set; } = new Nodetool.Types.Core.Datetime();

    public Nodetool.Types.Core.Date Process()
    {
        return default(Nodetool.Types.Core.Date);
    }
}
