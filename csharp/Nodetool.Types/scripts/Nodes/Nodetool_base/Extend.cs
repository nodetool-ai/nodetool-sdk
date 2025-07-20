using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class Extend
{
    [Key(0)]
    public object values { get; set; } = new List<object>();
    [Key(1)]
    public object other_values { get; set; } = new List<object>();

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
