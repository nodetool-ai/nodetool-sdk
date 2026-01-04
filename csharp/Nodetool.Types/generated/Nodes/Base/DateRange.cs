using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DateRange
{
    [Key(0)]
    public Nodetool.Types.Core.Datetime end_date { get; set; } = new Nodetool.Types.Core.Datetime();
    [Key(1)]
    public Nodetool.Types.Core.Datetime start_date { get; set; } = new Nodetool.Types.Core.Datetime();
    [Key(2)]
    public int step_days { get; set; } = 1;

    public object Process()
    {
        return default(object);
    }
}
