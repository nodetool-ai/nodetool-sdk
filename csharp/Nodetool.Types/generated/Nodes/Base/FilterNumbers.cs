using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FilterNumbers
{
    [Key(0)]
    public object filter_type { get; set; } = @"greater_than";
    [Key(1)]
    public double? value { get; set; } = null;
    [Key(2)]
    public object values { get; set; } = new();

    public object Process()
    {
        return default(object);
    }
}
