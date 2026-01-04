using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Sora2ProTextToVideo
{
    [Key(0)]
    public object aspect_ratio { get; set; } = @"landscape";
    [Key(1)]
    public int n_frames { get; set; } = 10;
    [Key(2)]
    public string prompt { get; set; } = @"A cinematic video with smooth motion, natural lighting, and high detail.";
    [Key(3)]
    public bool remove_watermark { get; set; } = true;

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}
