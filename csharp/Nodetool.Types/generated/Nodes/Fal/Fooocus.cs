using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class Fooocus
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public string negative_prompt { get; set; } = "";
    [Key(2)]
    public object styles { get; set; } = new List<object>();
    [Key(3)]
    public object performance { get; set; } = "PerformanceEnum.EXTREME_SPEED";
    [Key(4)]
    public double guidance_scale { get; set; } = 4.0;
    [Key(5)]
    public double sharpness { get; set; } = 2.0;
    [Key(6)]
    public string aspect_ratio { get; set; } = "1024x1024";
    [Key(7)]
    public object loras { get; set; } = new List<object>();
    [Key(8)]
    public object refiner_model { get; set; } = "RefinerModelEnum.NONE";
    [Key(9)]
    public double refiner_switch { get; set; } = 0.8;
    [Key(10)]
    public int seed { get; set; } = -1;
    [Key(11)]
    public Nodetool.Types.ImageRef control_image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(12)]
    public object control_type { get; set; } = "ControlTypeEnum.PYRA_CANNY";
    [Key(13)]
    public double control_image_weight { get; set; } = 1.0;
    [Key(14)]
    public double control_image_stop_at { get; set; } = 1.0;
    [Key(15)]
    public bool enable_safety_checker { get; set; } = true;

    public Nodetool.Types.ImageRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.ImageRef);
    }
}
