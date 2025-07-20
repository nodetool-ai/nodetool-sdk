using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_fal;

[MessagePackObject]
public class FastLCMDiffusion
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public object model_name { get; set; } = "ModelNameFastLCM.SDXL_BASE";
    [Key(2)]
    public string negative_prompt { get; set; } = "";
    [Key(3)]
    public object image_size { get; set; } = "ImageSizePreset.SQUARE_HD";
    [Key(4)]
    public int num_inference_steps { get; set; } = 6;
    [Key(5)]
    public double guidance_scale { get; set; } = 1.5;
    [Key(6)]
    public bool sync_mode { get; set; } = true;
    [Key(7)]
    public int num_images { get; set; } = 1;
    [Key(8)]
    public bool enable_safety_checker { get; set; } = true;
    [Key(9)]
    public object safety_checker_version { get; set; } = "SafetyCheckerVersion.V1";
    [Key(10)]
    public bool expand_prompt { get; set; } = false;
    [Key(11)]
    public double guidance_rescale { get; set; } = 0.0;
    [Key(12)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.ImageRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.ImageRef);
    }
}
