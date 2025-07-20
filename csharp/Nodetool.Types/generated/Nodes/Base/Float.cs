using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Constant;

[MessagePackObject]
public class Float
{
    [Key(0)]
    public double value { get; set; } = 0.0;

    public double Process()
    {
        // Implementation would be generated based on node logic
        return default(double);
    }
}
