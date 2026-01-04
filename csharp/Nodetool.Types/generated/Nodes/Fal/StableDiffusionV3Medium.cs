using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class StableDiffusionV3Medium
{
    [Key(0)]
    public bool enable_safety_checker { get; set; } = true;
    [Key(1)]
    public double guidance_scale { get; set; } = 5.0;
    [Key(2)]
    public object image_size { get; set; } = @"square_hd";
    [Key(3)]
    public string negative_prompt { get; set; } = @"";
    [Key(4)]
    public int num_images { get; set; } = 1;
    [Key(5)]
    public int num_inference_steps { get; set; } = 28;
    [Key(6)]
    public string prompt { get; set; } = @"";
    [Key(7)]
    public bool prompt_expansion { get; set; } = false;
    [Key(8)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
