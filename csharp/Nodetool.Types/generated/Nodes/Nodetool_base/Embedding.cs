using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class Embedding
{
    [Key(0)]
    public string input { get; set; } = "";
    [Key(1)]
    public object model { get; set; } = "EmbeddingModel.TEXT_EMBEDDING_3_SMALL";
    [Key(2)]
    public int chunk_size { get; set; } = 4096;

    public Nodetool.Types.NPArray Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.NPArray);
    }
}
