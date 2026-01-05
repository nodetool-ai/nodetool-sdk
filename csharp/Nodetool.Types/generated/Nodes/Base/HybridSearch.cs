using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class HybridSearch
{
    [Key(0)]
    public Nodetool.Types.Core.Collection collection { get; set; } = new Nodetool.Types.Core.Collection();
    [Key(1)]
    public double k_constant { get; set; } = 60.0;
    [Key(2)]
    public int min_keyword_length { get; set; } = 3;
    [Key(3)]
    public int n_results { get; set; } = 5;
    [Key(4)]
    public string text { get; set; } = @"";

    [MessagePackObject]
    public class HybridSearchOutput
    {
        [Key(0)]
        public object distances { get; set; }
        [Key(1)]
        public object documents { get; set; }
        [Key(2)]
        public object ids { get; set; }
        [Key(3)]
        public object metadatas { get; set; }
        [Key(4)]
        public object scores { get; set; }
    }

    public HybridSearchOutput Process()
    {
        return new HybridSearchOutput();
    }
}
