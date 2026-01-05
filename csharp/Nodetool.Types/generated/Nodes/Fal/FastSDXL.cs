using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class FastSDXL
{
    [Key(0)]
    public bool enable_safety_checker { get; set; } = true;
    [Key(1)]
    public bool expand_prompt { get; set; } = false;
    [Key(2)]
    public double guidance_scale { get; set; } = 7.5;
    [Key(3)]
    public object image_size { get; set; } = @"square_hd";
    [Key(4)]
    public object loras { get; set; } = new();
    [Key(5)]
    public string negative_prompt { get; set; } = @"";
    [Key(6)]
    public int num_images { get; set; } = 1;
    [Key(7)]
    public int num_inference_steps { get; set; } = 25;
    [Key(8)]
    public string prompt { get; set; } = @"";
    [Key(9)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
