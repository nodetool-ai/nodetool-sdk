using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class FastSDXLControlNetCanny
{
    [Key(0)]
    public Nodetool.Types.Core.ImageRef control_image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(1)]
    public bool enable_safety_checker { get; set; } = true;
    [Key(2)]
    public double guidance_scale { get; set; } = 7.5;
    [Key(3)]
    public object image_size { get; set; } = @"square_hd";
    [Key(4)]
    public string negative_prompt { get; set; } = @"";
    [Key(5)]
    public int num_inference_steps { get; set; } = 25;
    [Key(6)]
    public string prompt { get; set; } = @"";
    [Key(7)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
