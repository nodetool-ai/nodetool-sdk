using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Chroma;

[MessagePackObject]
public class IndexTextChunk
{
    [Key(0)]
    public Nodetool.Types.Collection collection { get; set; } = new Nodetool.Types.Collection();
    [Key(1)]
    public Nodetool.Types.TextChunk text_chunk { get; set; } = new Nodetool.Types.TextChunk();
    [Key(2)]
    public object metadata { get; set; } = new Dictionary<string, object>();

    public void Process()
    {
    }
}
