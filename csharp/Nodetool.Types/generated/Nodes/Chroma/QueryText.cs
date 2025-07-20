using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Chroma;

[MessagePackObject]
public class QueryText
{
    [Key(0)]
    public Nodetool.Types.Collection collection { get; set; } = new Nodetool.Types.Collection();
    [Key(1)]
    public string text { get; set; } = "";
    [Key(2)]
    public int n_results { get; set; } = 1;

    [MessagePackObject]
    public class QueryTextOutput
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

    public QueryTextOutput Process()
    {
        return new QueryTextOutput();
    }
}
