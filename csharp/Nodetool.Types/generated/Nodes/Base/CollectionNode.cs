using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Chroma;

[MessagePackObject]
public class CollectionNode
{
    [Key(0)]
    public string name { get; set; } = "";
    [Key(1)]
    public Nodetool.Types.LlamaModel embedding_model { get; set; } = new Nodetool.Types.LlamaModel();

    public Nodetool.Types.Collection Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.Collection);
    }
}
