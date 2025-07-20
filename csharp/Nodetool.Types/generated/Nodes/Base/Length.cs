using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.List;

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
