using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class QueryText
{
    [Key(0)]
    public Nodetool.Types.Core.Collection collection { get; set; } = new Nodetool.Types.Core.Collection();
    [Key(1)]
    public int n_results { get; set; } = 1;
    [Key(2)]
    public string text { get; set; } = @"";

    [MessagePackObject]
    public class QueryTextOutput
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

    public QueryTextOutput Process()
    {
        return new QueryTextOutput();
    }
}
