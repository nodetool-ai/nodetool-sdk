using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class FloatInput
{
    [Key(0)]
    public double value { get; set; } = 0.0;
    [Key(1)]
    public string name { get; set; } = "";
    [Key(2)]
    public string description { get; set; } = "";
    [Key(3)]
    public double min { get; set; } = 0;
    [Key(4)]
    public double max { get; set; } = 100;

    public double Process()
    {
        // Implementation would be generated based on node logic
        return default(double);
    }
}
