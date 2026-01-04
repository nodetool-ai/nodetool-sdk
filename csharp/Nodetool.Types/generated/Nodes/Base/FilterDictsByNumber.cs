using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FilterDictsByNumber
{
    [Key(0)]
    public object filter_type { get; set; } = @"greater_than";
    [Key(1)]
    public string key { get; set; } = @"";
    [Key(2)]
    public double? value { get; set; } = null;
    [Key(3)]
    public object values { get; set; } = new();

    public object Process()
    {
        return default(object);
    }
}
