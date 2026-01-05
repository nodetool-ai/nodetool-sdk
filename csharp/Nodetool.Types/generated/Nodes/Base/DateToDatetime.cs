using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DateToDatetime
{
    [Key(0)]
    public Nodetool.Types.Core.Date input_date { get; set; } = new Nodetool.Types.Core.Date();

    public Nodetool.Types.Core.Datetime Process()
    {
        return default(Nodetool.Types.Core.Datetime);
    }
}
