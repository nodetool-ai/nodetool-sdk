using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Wan26VideoToVideo
{
    [Key(0)]
    public object duration { get; set; } = @"5";
    [Key(1)]
    public string prompt { get; set; } = @"A cinematic video with smooth motion, natural lighting, and high detail.";
    [Key(2)]
    public object resolution { get; set; } = @"1080p";
    [Key(3)]
    public Nodetool.Types.Core.VideoRef video1 { get; set; } = new Nodetool.Types.Core.VideoRef();
    [Key(4)]
    public Nodetool.Types.Core.VideoRef video2 { get; set; } = new Nodetool.Types.Core.VideoRef();
    [Key(5)]
    public Nodetool.Types.Core.VideoRef video3 { get; set; } = new Nodetool.Types.Core.VideoRef();

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}
