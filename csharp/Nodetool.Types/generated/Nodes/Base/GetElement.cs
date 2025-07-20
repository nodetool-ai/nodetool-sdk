using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.List;

[MessagePackObject]
public class GetElement
{
    [Key(0)]
    public object values { get; set; } = new List<object>();
    [Key(1)]
    public int index { get; set; } = 0;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
