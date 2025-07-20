using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class ObjectDetection
{
    [Key(0)]
    public Nodetool.Types.HFObjectDetection model { get; set; } = new Nodetool.Types.HFObjectDetection();
    [Key(1)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(2)]
    public double threshold { get; set; } = 0.9;
    [Key(3)]
    public int top_k { get; set; } = 5;

    public object Process()
    {
        return default(object);
    }
}
