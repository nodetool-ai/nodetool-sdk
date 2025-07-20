using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class GoogleFinance
{
    [Key(0)]
    public string query { get; set; } = "";
    [Key(1)]
    public string window { get; set; } = "";

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
