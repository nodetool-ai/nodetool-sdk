using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class Reranker
{
    [Key(0)]
    public Nodetool.Types.HFReranker model { get; set; } = new Nodetool.Types.HFReranker();
    [Key(1)]
    public string query { get; set; } = "";
    [Key(2)]
    public object candidates { get; set; } = new List<object>();

    public object Process()
    {
        return default(object);
    }
}
