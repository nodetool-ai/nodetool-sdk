using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class RelativeTime
{
    [Key(0)]
    public int amount { get; set; } = 1;
    [Key(1)]
    public object direction { get; set; } = @"future";
    [Key(2)]
    public object unit { get; set; } = @"days";

    public Nodetool.Types.Core.Datetime Process()
    {
        return default(Nodetool.Types.Core.Datetime);
    }
}
