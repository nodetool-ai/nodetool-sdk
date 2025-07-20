using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class Switti
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public string negative_prompt { get; set; } = "";
    [Key(2)]
    public int sampling_top_k { get; set; } = 400;
    [Key(3)]
    public double sampling_top_p { get; set; } = 0.95;
    [Key(4)]
    public bool more_smooth { get; set; } = true;
    [Key(5)]
    public bool more_diverse { get; set; } = false;
    [Key(6)]
    public int smooth_start_si { get; set; } = 2;
    [Key(7)]
    public int turn_off_cfg_start_si { get; set; } = 8;
    [Key(8)]
    public double last_scale_temp { get; set; } = 0.1;
    [Key(9)]
    public int seed { get; set; } = -1;
    [Key(10)]
    public double guidance_scale { get; set; } = 6.0;
    [Key(11)]
    public bool enable_safety_checker { get; set; } = true;

    public Nodetool.Types.ImageRef Process()
    {
        return default(Nodetool.Types.ImageRef);
    }
}
