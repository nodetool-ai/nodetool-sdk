using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SplitRecursively
{
    [Key(0)]
    public int chunk_overlap { get; set; } = 200;
    [Key(1)]
    public int chunk_size { get; set; } = 1000;
    [Key(2)]
    public Nodetool.Types.Core.DocumentRef document { get; set; } = new Nodetool.Types.Core.DocumentRef();
    [Key(3)]
    public object separators { get; set; } = new();

    [MessagePackObject]
    public class SplitRecursivelyOutput
    {
        [Key(0)]
        public string source_id { get; set; }
        [Key(1)]
        public int start_index { get; set; }
        [Key(2)]
        public string text { get; set; }
    }

    public SplitRecursivelyOutput Process()
    {
        return new SplitRecursivelyOutput();
    }
}
