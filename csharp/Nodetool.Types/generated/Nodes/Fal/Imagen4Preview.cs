using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class Imagen4Preview
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public string negative_prompt { get; set; } = "";
    [Key(2)]
    public object aspect_ratio { get; set; } = "AspectRatio.RATIO_1_1";
    [Key(3)]
    public object image_size { get; set; } = "ImageSizePreset.LANDSCAPE_4_3";
    [Key(4)]
    public int num_inference_steps { get; set; } = 50;
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
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.ImageRef);
    }
}
