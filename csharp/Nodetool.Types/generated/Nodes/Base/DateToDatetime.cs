using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DateToDatetime
{
    [Key(0)]
    public Nodetool.Types.Date input_date { get; set; } = new Nodetool.Types.Date();

    public Nodetool.Types.Datetime Process()
    {
        return default(Nodetool.Types.Datetime);
    }
}
