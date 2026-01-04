using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class Reranker
{
    [Key(0)]
    public List<string> candidates { get; set; } = new();
    [Key(1)]
    public Nodetool.Types.Core.HFReranker model { get; set; } = new Nodetool.Types.Core.HFReranker();
    [Key(2)]
    public string query { get; set; } = @"";
}
