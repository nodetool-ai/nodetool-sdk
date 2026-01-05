using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FilterDictsByValue
{
    [Key(0)]
    public string criteria { get; set; } = @"";
    [Key(1)]
    public object filter_type { get; set; } = @"contains";
    [Key(2)]
    public string key { get; set; } = @"";
    [Key(3)]
    public object values { get; set; } = new();

    public object Process()
    {
        return default(object);
    }
}
