using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class RecursiveTextSplitter
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public string document_id { get; set; } = "";
    [Key(2)]
    public int chunk_size { get; set; } = 1000;
    [Key(3)]
    public int chunk_overlap { get; set; } = 200;
    [Key(4)]
    public object separators { get; set; } = new List<object>();

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
