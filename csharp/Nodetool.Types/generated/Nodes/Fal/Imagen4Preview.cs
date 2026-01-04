using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class Imagen4Preview
{
    [Key(0)]
    public object aspect_ratio { get; set; } = @"1:1";
    [Key(1)]
    public bool enable_safety_checker { get; set; } = true;
    [Key(2)]
    public double guidance_scale { get; set; } = 5.0;
    [Key(3)]
    public object image_size { get; set; } = @"landscape_4_3";
    [Key(4)]
    public string negative_prompt { get; set; } = @"";
    [Key(5)]
    public int num_images { get; set; } = 1;
    [Key(6)]
    public int num_inference_steps { get; set; } = 50;
    [Key(7)]
    public string prompt { get; set; } = @"";
    [Key(8)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
