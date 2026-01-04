using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class FluxLoraTTI
{
    [Key(0)]
    public bool enable_safety_checker { get; set; } = true;
    [Key(1)]
    public double guidance_scale { get; set; } = 7.5;
    [Key(2)]
    public object image_size { get; set; } = @"square_hd";
    [Key(3)]
    public object loras { get; set; } = new();
    [Key(4)]
    public object model_name { get; set; } = @"stabilityai/stable-diffusion-xl-base-1.0";
    [Key(5)]
    public string negative_prompt { get; set; } = @"";
    [Key(6)]
    public int num_inference_steps { get; set; } = 30;
    [Key(7)]
    public string prompt { get; set; } = @"";
    [Key(8)]
    public bool prompt_weighting { get; set; } = true;
    [Key(9)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
