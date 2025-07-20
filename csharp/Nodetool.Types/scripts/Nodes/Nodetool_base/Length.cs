using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class Length
{
    [Key(0)]
    public object values { get; set; } = new List<object>();

    public int Process()
    {
        // Implementation would be generated based on node logic
        return default(int);
    }
}
