using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.List;

[MessagePackObject]
public class Append
{
    [Key(0)]
    public object values { get; set; } = new List<object>();
    [Key(1)]
    public object value { get; set; } = null;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
