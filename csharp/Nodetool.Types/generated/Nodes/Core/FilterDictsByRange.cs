using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class FilterDictsByRange
{
    [Key(0)]
    public object values { get; set; } = new List<object>();
    [Key(1)]
    public string key { get; set; } = "";
    [Key(2)]
    public double min_value { get; set; } = 0;
    [Key(3)]
    public double max_value { get; set; } = 0;
    [Key(4)]
    public bool inclusive { get; set; } = true;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
