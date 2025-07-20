using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class FilterDictsByValue
{
    [Key(0)]
    public object values { get; set; } = new List<object>();
    [Key(1)]
    public string key { get; set; } = "";
    [Key(2)]
    public object filter_type { get; set; } = "FilterType.CONTAINS";
    [Key(3)]
    public string criteria { get; set; } = "";

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
