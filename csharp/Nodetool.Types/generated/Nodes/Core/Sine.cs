using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class Sine
{
    [Key(0)]
    public object angle_rad { get; set; } = 0.0;

    public double Process()
    {
        // Implementation would be generated based on node logic
        return default(double);
    }
}
