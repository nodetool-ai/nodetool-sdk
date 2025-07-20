using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class ParseList
{
    [Key(0)]
    public string json_string { get; set; } = "";

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
