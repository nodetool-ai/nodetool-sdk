using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Constant;

[MessagePackObject]
public class List
{
    [Key(0)]
    public object value { get; set; } = new List<object>();

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
