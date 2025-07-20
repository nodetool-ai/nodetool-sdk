using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class Compare
{
    [Key(0)]
    public object a { get; set; } = null;
    [Key(1)]
    public object b { get; set; } = null;
    [Key(2)]
    public object comparison { get; set; } = "Comparison.EQUAL";

    public bool Process()
    {
        // Implementation would be generated based on node logic
        return default(bool);
    }
}
