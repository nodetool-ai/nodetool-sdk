using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SemanticSplitter
{
    [Key(0)]
    public Nodetool.Types.LlamaModel embed_model { get; set; } = new Nodetool.Types.LlamaModel();
    [Key(1)]
    public string document_id { get; set; } = "";
    [Key(2)]
    public string text { get; set; } = "";
    [Key(3)]
    public int buffer_size { get; set; } = 1;
    [Key(4)]
    public int threshold { get; set; } = 95;

    public object Process()
    {
        return default(object);
    }
}
