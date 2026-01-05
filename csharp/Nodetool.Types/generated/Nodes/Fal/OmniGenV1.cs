using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class OmniGenV1
{
    [Key(0)]
    public bool enable_safety_checker { get; set; } = true;
    [Key(1)]
    public double guidance_scale { get; set; } = 3.0;
    [Key(2)]
    public object image_size { get; set; } = @"square_hd";
    [Key(3)]
    public double img_guidance_scale { get; set; } = 1.6;
    [Key(4)]
    public Nodetool.Types.Core.ImageRef input_image_1 { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(5)]
    public Nodetool.Types.Core.ImageRef input_image_2 { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(6)]
    public int num_images { get; set; } = 1;
    [Key(7)]
    public int num_inference_steps { get; set; } = 50;
    [Key(8)]
    public string prompt { get; set; } = @"";
    [Key(9)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
