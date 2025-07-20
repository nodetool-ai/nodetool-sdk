using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class HoursFromNow
{
    [Key(0)]
    public int hours { get; set; } = 1;

    public Nodetool.Types.Datetime Process()
    {
        return default(Nodetool.Types.Datetime);
    }
}
