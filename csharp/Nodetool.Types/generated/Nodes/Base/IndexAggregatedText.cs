using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class IndexAggregatedText
{
    [Key(0)]
    public object aggregation { get; set; }
    [Key(1)]
    public Nodetool.Types.Core.Collection collection { get; set; } = new Nodetool.Types.Core.Collection();
    [Key(2)]
    public int context_window { get; set; } = 4096;
    [Key(3)]
    public string document { get; set; } = @"";
    [Key(4)]
    public string document_id { get; set; } = @"";
    [Key(5)]
    public object metadata { get; set; } = new();
    [Key(6)]
    public object text_chunks { get; set; } = new();

    public void Process()
    {
    }
}
