using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class SentenceSplitter
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public string document_id { get; set; } = "";
    [Key(2)]
    public int chunk_size { get; set; } = 40;
    [Key(3)]
    public int chunk_overlap { get; set; } = 5;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
