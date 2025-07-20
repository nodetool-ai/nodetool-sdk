using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class MarkdownSplitter
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public string document_id { get; set; } = "";
    [Key(2)]
    public object headers_to_split_on { get; set; } = new List<object>();
    [Key(3)]
    public bool strip_headers { get; set; } = true;
    [Key(4)]
    public bool return_each_line { get; set; } = false;
    [Key(5)]
    public object chunk_size { get; set; } = null;
    [Key(6)]
    public int chunk_overlap { get; set; } = 30;

    public object Process()
    {
        return default(object);
    }
}
