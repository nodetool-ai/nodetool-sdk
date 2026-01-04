using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SeedanceV1ProTextToVideo
{
    [Key(0)]
    public object aspect_ratio { get; set; } = @"16:9";
    [Key(1)]
    public object duration { get; set; } = @"5";
    [Key(2)]
    public string prompt { get; set; } = @"A cinematic video with smooth motion, natural lighting, and high detail.";
    [Key(3)]
    public bool remove_watermark { get; set; } = true;
    [Key(4)]
    public object resolution { get; set; } = @"720p";

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}
