using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class AddTimeDelta
{
    [Key(0)]
    public Nodetool.Types.Datetime input_datetime { get; set; } = new Nodetool.Types.Datetime();
    [Key(1)]
    public int days { get; set; } = 0;
    [Key(2)]
    public int hours { get; set; } = 0;
    [Key(3)]
    public int minutes { get; set; } = 0;

    public Nodetool.Types.Datetime Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.Datetime);
    }
}
