using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class ZeroShotObjectDetection
{
    [Key(0)]
    public string candidate_labels { get; set; } = @"";
    [Key(1)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(2)]
    public Nodetool.Types.Core.HFZeroShotObjectDetection model { get; set; } = new Nodetool.Types.Core.HFZeroShotObjectDetection();
    [Key(3)]
    public double threshold { get; set; } = 0.1;
    [Key(4)]
    public int top_k { get; set; } = 5;

    public object Process()
    {
        return default(object);
    }
}
