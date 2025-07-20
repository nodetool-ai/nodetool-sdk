using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class GetJSONPathFloat
{
    [Key(0)]
    public object data { get; set; } = null;
    [Key(1)]
    public string path { get; set; } = "";
    [Key(2)]
    public double default { get; set; } = 0.0;

    public double Process()
    {
        // Implementation would be generated based on node logic
        return default(double);
    }
}
