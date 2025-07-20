using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_chroma;

[MessagePackObject]
public class IndexImages
{
    [Key(0)]
    public Nodetool.Types.Collection collection { get; set; } = new Nodetool.Types.Collection();
    [Key(1)]
    public object images { get; set; } = new List<object>();
    [Key(2)]
    public bool upsert { get; set; } = false;

    public void Process()
    {
        // Implementation would be generated based on node logic
    }
}
