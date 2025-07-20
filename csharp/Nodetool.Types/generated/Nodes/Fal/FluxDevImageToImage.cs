using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class FluxDevImageToImage
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(2)]
    public double strength { get; set; } = 0.95;
    [Key(3)]
    public int num_inference_steps { get; set; } = 40;
    [Key(4)]
    public double guidance_scale { get; set; } = 3.5;
    [Key(5)]
    public int seed { get; set; } = -1;
    [Key(6)]
    public bool enable_safety_checker { get; set; } = true;

    public Nodetool.Types.ImageRef Process()
    {
        return default(Nodetool.Types.ImageRef);
    }
}
