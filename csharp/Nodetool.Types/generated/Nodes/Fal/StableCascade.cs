using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class StableCascade
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public string negative_prompt { get; set; } = "";
    [Key(2)]
    public int first_stage_steps { get; set; } = 20;
    [Key(3)]
    public int second_stage_steps { get; set; } = 10;
    [Key(4)]
    public double guidance_scale { get; set; } = 4.0;
    [Key(5)]
    public double second_stage_guidance_scale { get; set; } = 4.0;
    [Key(6)]
    public object image_size { get; set; } = "ImageSizePreset.SQUARE_HD";
    [Key(7)]
    public int seed { get; set; } = -1;
    [Key(8)]
    public bool enable_safety_checker { get; set; } = true;

    public Nodetool.Types.ImageRef Process()
    {
        return default(Nodetool.Types.ImageRef);
    }
}
