using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class MapField
{
    [Key(0)]
    public object values { get; set; } = new List<object>();
    [Key(1)]
    public string field { get; set; } = "";
    [Key(2)]
    public object default { get; set; } = null;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
