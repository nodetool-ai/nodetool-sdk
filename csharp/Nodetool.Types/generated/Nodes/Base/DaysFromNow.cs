using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DaysFromNow
{
    [Key(0)]
    public int days { get; set; } = 1;

    public Nodetool.Types.Datetime Process()
    {
        return default(Nodetool.Types.Datetime);
    }
}
