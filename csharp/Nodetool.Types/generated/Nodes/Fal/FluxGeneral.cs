using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class FluxGeneral
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public object image_size { get; set; } = "ImageSizePreset.SQUARE_HD";
    [Key(2)]
    public int num_inference_steps { get; set; } = 28;
    [Key(3)]
    public double guidance_scale { get; set; } = 3.5;
    [Key(4)]
    public double real_cfg_scale { get; set; } = 3.5;
    [Key(5)]
    public bool use_real_cfg { get; set; } = false;
    [Key(6)]
    public int num_images { get; set; } = 1;
    [Key(7)]
    public int seed { get; set; } = -1;
    [Key(8)]
    public bool enable_safety_checker { get; set; } = true;
    [Key(9)]
    public double reference_strength { get; set; } = 0.65;
    [Key(10)]
    public double reference_end { get; set; } = 1.0;
    [Key(11)]
    public double base_shift { get; set; } = 0.5;
    [Key(12)]
    public double max_shift { get; set; } = 1.15;

    public Nodetool.Types.ImageRef Process()
    {
        return default(Nodetool.Types.ImageRef);
    }
}
