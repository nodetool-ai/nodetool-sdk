using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class StableDiffusionUpscale
{
    [Key(0)]
    public string prompt { get; set; } = "";
    [Key(1)]
    public string negative_prompt { get; set; } = "";
    [Key(2)]
    public int num_inference_steps { get; set; } = 25;
    [Key(3)]
    public double guidance_scale { get; set; } = 7.5;
    [Key(4)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(5)]
    public object scheduler { get; set; } = "StableDiffusionScheduler.HeunDiscreteScheduler";
    [Key(6)]
    public int seed { get; set; } = -1;
    [Key(7)]
    public bool enable_tiling { get; set; } = false;

    public Nodetool.Types.ImageRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.ImageRef);
    }
}
