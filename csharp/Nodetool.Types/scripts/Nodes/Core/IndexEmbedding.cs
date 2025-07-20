using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class IndexEmbedding
{
    [Key(0)]
    public Nodetool.Types.Collection collection { get; set; } = new Nodetool.Types.Collection();
    [Key(1)]
    public Nodetool.Types.NPArray embedding { get; set; } = new Nodetool.Types.NPArray();
    [Key(2)]
    public string index_id { get; set; } = "";
    [Key(3)]
    public object metadata { get; set; } = new Dictionary<string, object>();

    public void Process()
    {
        // Implementation would be generated based on node logic
    }
}
