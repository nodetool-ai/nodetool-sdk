using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class FastTurboDiffusion
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public object model_name { get; set; } = "ModelNameEnum.SDXL_TURBO";
    [Key(2)]
    public string negative_prompt { get; set; } = "";
    [Key(3)]
    public object image_size { get; set; } = "ImageSizePreset.SQUARE";
    [Key(4)]
    public int num_inference_steps { get; set; } = 2;
    [Key(5)]
    public double guidance_scale { get; set; } = 1.0;
    [Key(6)]
    public int seed { get; set; } = -1;
    [Key(7)]
    public bool enable_safety_checker { get; set; } = true;
    [Key(8)]
    public bool expand_prompt { get; set; } = false;

    public Nodetool.Types.ImageRef Process()
    {
        return default(Nodetool.Types.ImageRef);
    }
}
