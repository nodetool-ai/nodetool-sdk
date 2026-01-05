using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Embedding
{
    [Key(0)]
    public int chunk_size { get; set; } = 4096;
    [Key(1)]
    public string input { get; set; } = @"";
    [Key(2)]
    public object model { get; set; } = @"text-embedding-3-small";

    public Nodetool.Types.Core.NPArray Process()
    {
        return default(Nodetool.Types.Core.NPArray);
    }
}
