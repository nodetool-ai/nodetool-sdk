using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class FloatOutput
{
    [Key(0)]
    public double value { get; set; } = 0;
    [Key(1)]
    public string name { get; set; } = "";
    [Key(2)]
    public string description { get; set; } = "";

    public double Process()
    {
        // Implementation would be generated based on node logic
        return default(double);
    }
}
