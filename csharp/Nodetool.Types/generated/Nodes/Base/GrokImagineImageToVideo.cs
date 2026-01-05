using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GrokImagineImageToVideo
{
    [Key(0)]
    public object duration { get; set; } = @"medium";
    [Key(1)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(2)]
    public string prompt { get; set; } = @"A cinematic video with smooth motion, natural lighting, and high detail.";

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}
