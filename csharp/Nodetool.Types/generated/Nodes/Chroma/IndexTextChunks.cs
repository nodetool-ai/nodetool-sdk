using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Chroma;

[MessagePackObject]
public class IndexTextChunks
{
    [Key(0)]
    public Nodetool.Types.Collection collection { get; set; } = new Nodetool.Types.Collection();
    [Key(1)]
    public object text_chunks { get; set; } = new List<object>();

    public void Process()
    {
        // Implementation would be generated based on node logic
    }
}
