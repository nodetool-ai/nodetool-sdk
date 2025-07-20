using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DateRange
{
    [Key(0)]
    public Nodetool.Types.Datetime start_date { get; set; } = new Nodetool.Types.Datetime();
    [Key(1)]
    public Nodetool.Types.Datetime end_date { get; set; } = new Nodetool.Types.Datetime();
    [Key(2)]
    public int step_days { get; set; } = 1;

    public object Process()
    {
        return default(object);
    }
}
