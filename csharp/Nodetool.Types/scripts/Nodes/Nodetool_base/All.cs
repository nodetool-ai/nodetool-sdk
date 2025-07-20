using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class All
{
    [Key(0)]
    public object values { get; set; } = new List<object>();

    public bool Process()
    {
        // Implementation would be generated based on node logic
        return default(bool);
    }
}
