using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class Sqrt
{
    [Key(0)]
    public object x { get; set; } = 1.0;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
