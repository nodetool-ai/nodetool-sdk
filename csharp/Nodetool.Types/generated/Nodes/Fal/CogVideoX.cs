using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class CogVideoX
{
    [Key(0)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(1)]
    public string prompt { get; set; } = "";
    [Key(2)]
    public object video_size { get; set; }
    [Key(3)]
    public string negative_prompt { get; set; } = "Distorted, discontinuous, Ugly, blurry, low resolution, motionless, static, disfigured, disconnected limbs, Ugly faces, incomplete arms";
    [Key(4)]
    public int num_inference_steps { get; set; } = 50;
    [Key(5)]
    public double guidance_scale { get; set; } = 7.0;
    [Key(6)]
    public bool use_rife { get; set; } = true;
    [Key(7)]
    public int export_fps { get; set; } = 16;
    [Key(8)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.VideoRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.VideoRef);
    }
}
