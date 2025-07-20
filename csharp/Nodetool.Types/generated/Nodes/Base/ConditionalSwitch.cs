using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ConditionalSwitch
{
    [Key(0)]
    public bool condition { get; set; } = false;
    [Key(1)]
    public object if_true { get; set; } = null;
    [Key(2)]
    public object if_false { get; set; } = null;

    public object Process()
    {
        return default(object);
    }
}
