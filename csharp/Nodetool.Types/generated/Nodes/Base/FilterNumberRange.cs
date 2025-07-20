using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

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
        return default(object);
    }
}
