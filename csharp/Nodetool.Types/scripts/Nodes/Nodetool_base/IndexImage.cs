using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class IndexImage
{
    [Key(0)]
    public Nodetool.Types.Collection collection { get; set; } = new Nodetool.Types.Collection();
    [Key(1)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(2)]
    public object metadata { get; set; } = new Dictionary<string, object>();

    public void Process()
    {
        // Implementation would be generated based on node logic
    }
}
