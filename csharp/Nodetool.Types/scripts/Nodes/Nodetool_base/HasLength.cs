using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class HasLength
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public object min_length { get; set; } = null;
    [Key(2)]
    public object max_length { get; set; } = null;
    [Key(3)]
    public object exact_length { get; set; } = null;

    public bool Process()
    {
        // Implementation would be generated based on node logic
        return default(bool);
    }
}
