using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class FluxGeneral
{
    [Key(0)]
    public double base_shift { get; set; } = 0.5;
    [Key(1)]
    public bool enable_safety_checker { get; set; } = true;
    [Key(2)]
    public double guidance_scale { get; set; } = 3.5;
    [Key(3)]
    public object image_size { get; set; } = @"square_hd";
    [Key(4)]
    public double max_shift { get; set; } = 1.15;
    [Key(5)]
    public int num_images { get; set; } = 1;
    [Key(6)]
    public int num_inference_steps { get; set; } = 28;
    [Key(7)]
    public string prompt { get; set; } = @"";
    [Key(8)]
    public double real_cfg_scale { get; set; } = 3.5;
    [Key(9)]
    public double reference_end { get; set; } = 1.0;
    [Key(10)]
    public double reference_strength { get; set; } = 0.65;
    [Key(11)]
    public int seed { get; set; } = -1;
    [Key(12)]
    public bool use_real_cfg { get; set; } = false;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
