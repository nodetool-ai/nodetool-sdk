using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class TextToVideo
{
    [Key(0)]
    public object aspect_ratio { get; set; } = @"16:9";
    [Key(1)]
    public double guidance_scale { get; set; } = 7.5;
    [Key(2)]
    public Nodetool.Types.Core.VideoModel model { get; set; } = new Nodetool.Types.Core.VideoModel();
    [Key(3)]
    public string negative_prompt { get; set; } = @"";
    [Key(4)]
    public int num_frames { get; set; } = 60;
    [Key(5)]
    public int num_inference_steps { get; set; } = 30;
    [Key(6)]
    public string prompt { get; set; } = @"A cat playing with a ball of yarn";
    [Key(7)]
    public object resolution { get; set; } = @"720p";
    [Key(8)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}
