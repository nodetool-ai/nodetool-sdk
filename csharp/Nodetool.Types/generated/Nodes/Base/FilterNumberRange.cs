using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FilterNumberRange
{
    [Key(0)]
    public bool inclusive { get; set; } = true;
    [Key(1)]
    public double max_value { get; set; } = 0;
    [Key(2)]
    public double min_value { get; set; } = 0;
    [Key(3)]
    public object values { get; set; } = new();

    public object Process()
    {
        return default(object);
    }
}
