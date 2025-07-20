using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class Power
{
    [Key(0)]
    public object base { get; set; } = 1.0;
    [Key(1)]
    public object exponent { get; set; } = 2.0;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
