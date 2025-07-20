using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.List;

[MessagePackObject]
public class Difference
{
    [Key(0)]
    public object list1 { get; set; } = new List<object>();
    [Key(1)]
    public object list2 { get; set; } = new List<object>();

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
