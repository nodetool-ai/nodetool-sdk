using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class FilterNumbers
{
    [Key(0)]
    public object values { get; set; } = new List<object>();
    [Key(1)]
    public object filter_type { get; set; } = "FilterNumberType.GREATER_THAN";
    [Key(2)]
    public object value { get; set; } = null;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
