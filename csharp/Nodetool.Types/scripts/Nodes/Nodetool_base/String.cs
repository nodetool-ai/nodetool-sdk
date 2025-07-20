using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class String
{
    [Key(0)]
    public string value { get; set; } = "";

    public string Process()
    {
        // Implementation would be generated based on node logic
        return default(string);
    }
}
