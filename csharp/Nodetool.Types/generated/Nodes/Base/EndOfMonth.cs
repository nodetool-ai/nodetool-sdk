using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class EndOfMonth
{
    [Key(0)]
    public Nodetool.Types.Datetime input_datetime { get; set; } = new Nodetool.Types.Datetime();

    public Nodetool.Types.Datetime Process()
    {
        return default(Nodetool.Types.Datetime);
    }
}
