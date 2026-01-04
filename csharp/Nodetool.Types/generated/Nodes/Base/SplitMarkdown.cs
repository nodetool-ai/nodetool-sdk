using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SplitMarkdown
{
    [Key(0)]
    public int chunk_overlap { get; set; } = 30;
    [Key(1)]
    public int? chunk_size { get; set; } = null;
    [Key(2)]
    public Nodetool.Types.Core.DocumentRef document { get; set; } = new Nodetool.Types.Core.DocumentRef();
    [Key(3)]
    public object headers_to_split_on { get; set; } = new();
    [Key(4)]
    public bool return_each_line { get; set; } = false;
    [Key(5)]
    public bool strip_headers { get; set; } = true;

    [MessagePackObject]
    public class SplitMarkdownOutput
    {
        [Key(0)]
        public string source_id { get; set; }
        [Key(1)]
        public int start_index { get; set; }
        [Key(2)]
        public string text { get; set; }
    }

    public SplitMarkdownOutput Process()
    {
        return new SplitMarkdownOutput();
    }
}
