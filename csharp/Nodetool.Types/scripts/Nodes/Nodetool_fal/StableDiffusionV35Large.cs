using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_fal;

[MessagePackObject]
public class StableDiffusionV35Large
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public string negative_prompt { get; set; } = "";
    [Key(2)]
    public int num_inference_steps { get; set; } = 28;
    [Key(3)]
    public double guidance_scale { get; set; } = 3.5;
    [Key(4)]
    public object image_size { get; set; } = "ImageSizePreset.LANDSCAPE_4_3";
    [Key(5)]
    public int seed { get; set; } = -1;
    [Key(6)]
    public bool enable_safety_checker { get; set; } = true;

    public Nodetool.Types.ImageRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.ImageRef);
    }
}
