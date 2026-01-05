using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SplitHTML
{
    [Key(0)]
    public Nodetool.Types.Core.DocumentRef document { get; set; } = new Nodetool.Types.Core.DocumentRef();

    [MessagePackObject]
    public class SplitHTMLOutput
    {
        [Key(0)]
        public string source_id { get; set; }
        [Key(1)]
        public int start_index { get; set; }
        [Key(2)]
        public string text { get; set; }
    }

    public SplitHTMLOutput Process()
    {
        return new SplitHTMLOutput();
    }
}
