using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_fal;

[MessagePackObject]
public class SanaV1
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public string negative_prompt { get; set; } = "";
    [Key(2)]
    public object image_size { get; set; } = "ImageSizePreset.SQUARE_HD";
    [Key(3)]
    public int num_inference_steps { get; set; } = 18;
    [Key(4)]
    public double guidance_scale { get; set; } = 5.0;
    [Key(5)]
    public int num_images { get; set; } = 1;
    [Key(6)]
    public int seed { get; set; } = -1;
    [Key(7)]
    public bool enable_safety_checker { get; set; } = true;

    public Nodetool.Types.ImageRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.ImageRef);
    }
}
