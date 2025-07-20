using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Sort
{
    [Key(0)]
    public object values { get; set; } = new List<object>();
    [Key(1)]
    public object order { get; set; } = "SortOrder.ASCENDING";

    public object Process()
    {
        return default(object);
    }
}
