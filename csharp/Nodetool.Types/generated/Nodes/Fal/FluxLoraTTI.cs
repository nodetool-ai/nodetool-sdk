using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class FluxLoraTTI
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public string negative_prompt { get; set; } = "";
    [Key(2)]
    public object model_name { get; set; } = "LoraModel.SDXL_BASE";
    [Key(3)]
    public object image_size { get; set; } = "ImageSizePreset.SQUARE_HD";
    [Key(4)]
    public int num_inference_steps { get; set; } = 30;
    [Key(5)]
    public double guidance_scale { get; set; } = 7.5;
    [Key(6)]
    public object loras { get; set; } = new List<object>();
    [Key(7)]
    public bool prompt_weighting { get; set; } = true;
    [Key(8)]
    public int seed { get; set; } = -1;
    [Key(9)]
    public bool enable_safety_checker { get; set; } = true;

    public Nodetool.Types.ImageRef Process()
    {
        return default(Nodetool.Types.ImageRef);
    }
}
