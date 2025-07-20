using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_chroma;

[MessagePackObject]
public class HybridSearch
{
    [Key(0)]
    public Nodetool.Types.Collection collection { get; set; } = new Nodetool.Types.Collection();
    [Key(1)]
    public string text { get; set; } = "";
    [Key(2)]
    public int n_results { get; set; } = 5;
    [Key(3)]
    public double k_constant { get; set; } = 60.0;
    [Key(4)]
    public int min_keyword_length { get; set; } = 3;

    [MessagePackObject]
    public class HybridSearchOutput
    {
        [Key(0)]
        public object ids { get; set; }
        [Key(1)]
        public object documents { get; set; }
        [Key(2)]
        public object metadatas { get; set; }
        [Key(3)]
        public object distances { get; set; }
        [Key(4)]
        public object scores { get; set; }
    }

    public HybridSearchOutput Process()
    {
        // Implementation would be generated based on node logic
        return new HybridSearchOutput();
    }
}
