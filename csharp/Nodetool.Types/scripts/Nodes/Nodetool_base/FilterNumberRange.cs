using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class FilterNumberRange
{
    [Key(0)]
    public object values { get; set; } = new List<object>();
    [Key(1)]
    public double min_value { get; set; } = 0;
    [Key(2)]
    public double max_value { get; set; } = 0;
    [Key(3)]
    public bool inclusive { get; set; } = true;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
