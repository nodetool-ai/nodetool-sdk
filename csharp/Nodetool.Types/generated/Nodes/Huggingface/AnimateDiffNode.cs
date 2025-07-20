using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Huggingface;

[MessagePackObject]
public class AnimateDiffNode
{
    [Key(0)]
    public Nodetool.Types.HFStableDiffusion model { get; set; } = new Nodetool.Types.HFStableDiffusion();
    [Key(1)]
    public string prompt { get; set; } = "masterpiece, bestquality, highlydetailed, ultradetailed, sunset, orange sky, warm lighting, fishing boats, ocean waves seagulls, rippling water, wharf, silhouette, serene atmosphere, dusk, evening glow, golden hour, coastal landscape, seaside scenery";
    [Key(2)]
    public string negative_prompt { get; set; } = "bad quality, worse quality";
    [Key(3)]
    public int num_frames { get; set; } = 16;
    [Key(4)]
    public double guidance_scale { get; set; } = 7.5;
    [Key(5)]
    public int num_inference_steps { get; set; } = 25;
    [Key(6)]
    public int seed { get; set; } = 42;

    public Nodetool.Types.VideoRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.VideoRef);
    }
}
