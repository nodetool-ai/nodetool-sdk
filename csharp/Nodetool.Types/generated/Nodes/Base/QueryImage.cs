using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class QueryImage
{
    [Key(0)]
    public Nodetool.Types.Core.Collection collection { get; set; } = new Nodetool.Types.Core.Collection();
    [Key(1)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(2)]
    public int n_results { get; set; } = 1;

    [MessagePackObject]
    public class QueryImageOutput
    {
        [Key(0)]
        public object distances { get; set; }
        [Key(1)]
        public object documents { get; set; }
        [Key(2)]
        public object ids { get; set; }
        [Key(3)]
        public object metadatas { get; set; }
    }

    public QueryImageOutput Process()
    {
        return new QueryImageOutput();
    }
}
