using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class LCMDiffusion
{
    [Key(0)]
    public bool enable_safety_checker { get; set; } = true;
    [Key(1)]
    public double guidance_scale { get; set; } = 1.0;
    [Key(2)]
    public object image_size { get; set; } = @"square";
    [Key(3)]
    public object model { get; set; } = @"sdv1-5";
    [Key(4)]
    public string negative_prompt { get; set; } = @"";
    [Key(5)]
    public int num_inference_steps { get; set; } = 4;
    [Key(6)]
    public string prompt { get; set; } = @"";
    [Key(7)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
