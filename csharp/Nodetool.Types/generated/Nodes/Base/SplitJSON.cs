using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SplitJSON
{
    [Key(0)]
    public Nodetool.Types.Core.DocumentRef document { get; set; } = new Nodetool.Types.Core.DocumentRef();
    [Key(1)]
    public bool include_metadata { get; set; } = true;
    [Key(2)]
    public bool include_prev_next_rel { get; set; } = true;

    [MessagePackObject]
    public class SplitJSONOutput
    {
        [Key(0)]
        public string source_id { get; set; }
        [Key(1)]
        public int start_index { get; set; }
        [Key(2)]
        public string text { get; set; }
    }

    public SplitJSONOutput Process()
    {
        return new SplitJSONOutput();
    }
}
