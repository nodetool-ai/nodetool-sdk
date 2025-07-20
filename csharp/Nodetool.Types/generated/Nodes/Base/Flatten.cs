using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Flatten
{
    [Key(0)]
    public object values { get; set; } = new List<object>();
    [Key(1)]
    public int max_depth { get; set; } = -1;

    public object Process()
    {
        return default(object);
    }
}
