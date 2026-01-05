using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class FluxLoraInpainting
{
    [Key(0)]
    public bool enable_safety_checker { get; set; } = true;
    [Key(1)]
    public double guidance_scale { get; set; } = 3.5;
    [Key(2)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(3)]
    public object loras { get; set; } = new();
    [Key(4)]
    public Nodetool.Types.Core.ImageRef mask { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(5)]
    public int num_inference_steps { get; set; } = 28;
    [Key(6)]
    public string prompt { get; set; } = @"";
    [Key(7)]
    public int seed { get; set; } = -1;
    [Key(8)]
    public double strength { get; set; } = 0.85;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
