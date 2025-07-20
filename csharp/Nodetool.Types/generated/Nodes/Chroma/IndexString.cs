using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Chroma;

[MessagePackObject]
public class IndexString
{
    [Key(0)]
    public Nodetool.Types.Collection collection { get; set; } = new Nodetool.Types.Collection();
    [Key(1)]
    public string text { get; set; } = "";
    [Key(2)]
    public string document_id { get; set; } = "";
    [Key(3)]
    public object metadata { get; set; } = new Dictionary<string, object>();

    public void Process()
    {
    }
}
