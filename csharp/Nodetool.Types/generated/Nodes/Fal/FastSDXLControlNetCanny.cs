using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class FastSDXLControlNetCanny
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public Nodetool.Types.ImageRef control_image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(2)]
    public string negative_prompt { get; set; } = "";
    [Key(3)]
    public double guidance_scale { get; set; } = 7.5;
    [Key(4)]
    public int num_inference_steps { get; set; } = 25;
    [Key(5)]
    public object image_size { get; set; } = "ImageSizePreset.SQUARE_HD";
    [Key(6)]
    public int seed { get; set; } = -1;
    [Key(7)]
    public bool enable_safety_checker { get; set; } = true;

    public Nodetool.Types.ImageRef Process()
    {
        return default(Nodetool.Types.ImageRef);
    }
}
