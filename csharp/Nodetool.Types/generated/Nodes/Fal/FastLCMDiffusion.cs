using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class FastLCMDiffusion
{
    [Key(0)]
    public bool enable_safety_checker { get; set; } = true;
    [Key(1)]
    public bool expand_prompt { get; set; } = false;
    [Key(2)]
    public double guidance_rescale { get; set; } = 0.0;
    [Key(3)]
    public double guidance_scale { get; set; } = 1.5;
    [Key(4)]
    public object image_size { get; set; } = @"square_hd";
    [Key(5)]
    public object model_name { get; set; } = @"stabilityai/stable-diffusion-xl-base-1.0";
    [Key(6)]
    public string negative_prompt { get; set; } = @"";
    [Key(7)]
    public int num_images { get; set; } = 1;
    [Key(8)]
    public int num_inference_steps { get; set; } = 6;
    [Key(9)]
    public string prompt { get; set; } = @"";
    [Key(10)]
    public object safety_checker_version { get; set; } = @"v1";
    [Key(11)]
    public int seed { get; set; } = -1;
    [Key(12)]
    public bool sync_mode { get; set; } = true;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
