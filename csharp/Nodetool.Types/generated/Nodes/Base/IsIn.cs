using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class IsIn
{
    [Key(0)]
    public object options { get; set; } = new();
    [Key(1)]
    public object value { get; set; } = null;

    public bool Process()
    {
        return default(bool);
    }
}
