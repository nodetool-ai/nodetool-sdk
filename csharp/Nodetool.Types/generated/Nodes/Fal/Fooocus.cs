using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class Fooocus
{
    [Key(0)]
    public string aspect_ratio { get; set; } = @"1024x1024";
    [Key(1)]
    public Nodetool.Types.Core.ImageRef control_image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(2)]
    public double control_image_stop_at { get; set; } = 1.0;
    [Key(3)]
    public double control_image_weight { get; set; } = 1.0;
    [Key(4)]
    public object control_type { get; set; } = @"PyraCanny";
    [Key(5)]
    public bool enable_safety_checker { get; set; } = true;
    [Key(6)]
    public double guidance_scale { get; set; } = 4.0;
    [Key(7)]
    public object loras { get; set; } = new();
    [Key(8)]
    public string negative_prompt { get; set; } = @"";
    [Key(9)]
    public object performance { get; set; } = @"Extreme Speed";
    [Key(10)]
    public string prompt { get; set; } = @"";
    [Key(11)]
    public object refiner_model { get; set; } = @"None";
    [Key(12)]
    public double refiner_switch { get; set; } = 0.8;
    [Key(13)]
    public int seed { get; set; } = -1;
    [Key(14)]
    public double sharpness { get; set; } = 2.0;
    [Key(15)]
    public object styles { get; set; } = new();

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
