using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class QueryImage
{
    [Key(0)]
    public Nodetool.Types.Collection collection { get; set; } = new Nodetool.Types.Collection();
    [Key(1)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(2)]
    public int n_results { get; set; } = 1;

    [MessagePackObject]
    public class QueryImageOutput
    {
        [Key(0)]
        public object ids { get; set; }
        [Key(1)]
        public object documents { get; set; }
        [Key(2)]
        public object metadatas { get; set; }
        [Key(3)]
        public object distances { get; set; }
    }

    public QueryImageOutput Process()
    {
        // Implementation would be generated based on node logic
        return new QueryImageOutput();
    }
}
