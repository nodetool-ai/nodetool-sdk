using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class CogVideoX
{
    [Key(0)]
    public int export_fps { get; set; } = 16;
    [Key(1)]
    public double guidance_scale { get; set; } = 7.0;
    [Key(2)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(3)]
    public string negative_prompt { get; set; } = @"Distorted, discontinuous, Ugly, blurry, low resolution, motionless, static, disfigured, disconnected limbs, Ugly faces, incomplete arms";
    [Key(4)]
    public int num_inference_steps { get; set; } = 50;
    [Key(5)]
    public string prompt { get; set; } = @"";
    [Key(6)]
    public int seed { get; set; } = -1;
    [Key(7)]
    public bool use_rife { get; set; } = true;
    [Key(8)]
    public object video_size { get; set; }

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}
