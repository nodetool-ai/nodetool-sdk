using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class StableDiffusionV3Medium
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public string negative_prompt { get; set; } = "";
    [Key(2)]
    public bool prompt_expansion { get; set; } = false;
    [Key(3)]
    public object image_size { get; set; } = "ImageSizePreset.SQUARE_HD";
    [Key(4)]
    public int num_inference_steps { get; set; } = 28;
    [Key(5)]
    public double guidance_scale { get; set; } = 5.0;
    [Key(6)]
    public int num_images { get; set; } = 1;
    [Key(7)]
    public int seed { get; set; } = -1;
    [Key(8)]
    public bool enable_safety_checker { get; set; } = true;

    public Nodetool.Types.ImageRef Process()
    {
        return default(Nodetool.Types.ImageRef);
    }
}
