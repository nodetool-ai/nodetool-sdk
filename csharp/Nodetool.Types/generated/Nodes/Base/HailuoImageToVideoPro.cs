using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class HailuoImageToVideoPro
{
    [Key(0)]
    public object duration { get; set; } = @"6";
    [Key(1)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(2)]
    public string prompt { get; set; } = @"A cinematic video with smooth motion, natural lighting, and high detail.";
    [Key(3)]
    public object resolution { get; set; } = @"768P";

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}
