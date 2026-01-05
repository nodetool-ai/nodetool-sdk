using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class BoundaryTime
{
    [Key(0)]
    public object boundary { get; set; } = @"start";
    [Key(1)]
    public Nodetool.Types.Core.Datetime input_datetime { get; set; } = new Nodetool.Types.Core.Datetime();
    [Key(2)]
    public object period { get; set; } = @"day";
    [Key(3)]
    public bool start_monday { get; set; } = true;

    public Nodetool.Types.Core.Datetime Process()
    {
        return default(Nodetool.Types.Core.Datetime);
    }
}
