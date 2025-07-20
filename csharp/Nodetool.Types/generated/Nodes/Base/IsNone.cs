using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class IsNone
{
    [Key(0)]
    public object value { get; set; } = null;

    public bool Process()
    {
        return default(bool);
    }
}
