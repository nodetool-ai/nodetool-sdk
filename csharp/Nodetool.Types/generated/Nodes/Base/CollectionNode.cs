using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class CollectionNode
{
    [Key(0)]
    public Nodetool.Types.Core.LlamaModel embedding_model { get; set; } = new Nodetool.Types.Core.LlamaModel();
    [Key(1)]
    public string name { get; set; } = @"";

    public Nodetool.Types.Core.Collection Process()
    {
        return default(Nodetool.Types.Core.Collection);
    }
}
