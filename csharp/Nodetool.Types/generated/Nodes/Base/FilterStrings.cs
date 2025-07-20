using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.List;

[MessagePackObject]
public class FilterStrings
{
    [Key(0)]
    public object values { get; set; } = new List<object>();
    [Key(1)]
    public object filter_type { get; set; } = "FilterType.CONTAINS";
    [Key(2)]
    public string criteria { get; set; } = "";

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
