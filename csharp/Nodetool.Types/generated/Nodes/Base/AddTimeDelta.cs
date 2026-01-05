using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class AddTimeDelta
{
    [Key(0)]
    public int days { get; set; } = 0;
    [Key(1)]
    public int hours { get; set; } = 0;
    [Key(2)]
    public Nodetool.Types.Core.Datetime input_datetime { get; set; } = new Nodetool.Types.Core.Datetime();
    [Key(3)]
    public int minutes { get; set; } = 0;

    public Nodetool.Types.Core.Datetime Process()
    {
        return default(Nodetool.Types.Core.Datetime);
    }
}
