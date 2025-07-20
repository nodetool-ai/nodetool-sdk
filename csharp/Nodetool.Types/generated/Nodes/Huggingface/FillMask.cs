using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class FillMask
{
    [Key(0)]
    public Nodetool.Types.HFFillMask model { get; set; } = new Nodetool.Types.HFFillMask();
    [Key(1)]
    public string inputs { get; set; } = "The capital of France is [MASK].";
    [Key(2)]
    public int top_k { get; set; } = 5;

    public object Process()
    {
        return default(object);
    }
}
