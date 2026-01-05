using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class StableCascade
{
    [Key(0)]
    public bool enable_safety_checker { get; set; } = true;
    [Key(1)]
    public int first_stage_steps { get; set; } = 20;
    [Key(2)]
    public double guidance_scale { get; set; } = 4.0;
    [Key(3)]
    public object image_size { get; set; } = @"square_hd";
    [Key(4)]
    public string negative_prompt { get; set; } = @"";
    [Key(5)]
    public string prompt { get; set; } = @"";
    [Key(6)]
    public double second_stage_guidance_scale { get; set; } = 4.0;
    [Key(7)]
    public int second_stage_steps { get; set; } = 10;
    [Key(8)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
