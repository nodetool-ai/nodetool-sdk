using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class All
{
    [Key(0)]
    public object values { get; set; } = new();

    public bool Process()
    {
        return default(bool);
    }
}
