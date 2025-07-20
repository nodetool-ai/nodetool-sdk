using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class IndexAggregatedText
{
    [Key(0)]
    public Nodetool.Types.Collection collection { get; set; } = new Nodetool.Types.Collection();
    [Key(1)]
    public string document { get; set; } = "";
    [Key(2)]
    public string document_id { get; set; } = "";
    [Key(3)]
    public object metadata { get; set; } = new Dictionary<string, object>();
    [Key(4)]
    public object text_chunks { get; set; } = new List<object>();
    [Key(5)]
    public int context_window { get; set; } = 4096;
    [Key(6)]
    public object aggregation { get; set; }

    public void Process()
    {
        // Implementation would be generated based on node logic
    }
}
