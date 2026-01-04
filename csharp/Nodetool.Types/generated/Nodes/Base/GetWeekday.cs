using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GetWeekday
{
    [Key(0)]
    public bool as_name { get; set; } = true;
    [Key(1)]
    public Nodetool.Types.Core.Datetime input_datetime { get; set; } = new Nodetool.Types.Core.Datetime();

    public object Process()
    {
        return default(object);
    }
}
