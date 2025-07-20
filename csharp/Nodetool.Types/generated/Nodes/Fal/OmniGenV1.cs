using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class OmniGenV1
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public Nodetool.Types.ImageRef input_image_1 { get; set; } = new Nodetool.Types.ImageRef();
    [Key(2)]
    public Nodetool.Types.ImageRef input_image_2 { get; set; } = new Nodetool.Types.ImageRef();
    [Key(3)]
    public object image_size { get; set; } = "ImageSizePreset.SQUARE_HD";
    [Key(4)]
    public int num_inference_steps { get; set; } = 50;
    [Key(5)]
    public double guidance_scale { get; set; } = 3.0;
    [Key(6)]
    public double img_guidance_scale { get; set; } = 1.6;
    [Key(7)]
    public int num_images { get; set; } = 1;
    [Key(8)]
    public int seed { get; set; } = -1;
    [Key(9)]
    public bool enable_safety_checker { get; set; } = true;

    public Nodetool.Types.ImageRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.ImageRef);
    }
}
