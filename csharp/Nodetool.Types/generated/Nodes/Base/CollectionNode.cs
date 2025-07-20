using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class CollectionNode
{
    [Key(0)]
    public string name { get; set; } = "";
    [Key(1)]
    public Nodetool.Types.LlamaModel embedding_model { get; set; } = new Nodetool.Types.LlamaModel();

    public Nodetool.Types.Collection Process()
    {
        return default(Nodetool.Types.Collection);
    }
}
