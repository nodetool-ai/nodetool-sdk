using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class ZeroShotObjectDetection
{
    [Key(0)]
    public Nodetool.Types.HFZeroShotObjectDetection model { get; set; } = new Nodetool.Types.HFZeroShotObjectDetection();
    [Key(1)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(2)]
    public double threshold { get; set; } = 0.1;
    [Key(3)]
    public int top_k { get; set; } = 5;
    [Key(4)]
    public string candidate_labels { get; set; } = "";

    public object Process()
    {
        return default(object);
    }
}
