using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class LTXVideo
{
    [Key(0)]
    public double guidance_scale { get; set; } = 3.0;
    [Key(1)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(2)]
    public string negative_prompt { get; set; } = @"low quality, worst quality, deformed, distorted, disfigured, motion smear, motion artifacts, fused fingers, bad anatomy, weird hand, ugly";
    [Key(3)]
    public int num_inference_steps { get; set; } = 30;
    [Key(4)]
    public string prompt { get; set; } = @"";
    [Key(5)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}
