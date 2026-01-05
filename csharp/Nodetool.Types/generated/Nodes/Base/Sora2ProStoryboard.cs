using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Sora2ProStoryboard
{
    [Key(0)]
    public object aspect_ratio { get; set; } = @"landscape";
    [Key(1)]
    public Nodetool.Types.Core.ImageRef image1 { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(2)]
    public Nodetool.Types.Core.ImageRef image2 { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(3)]
    public Nodetool.Types.Core.ImageRef image3 { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(4)]
    public int n_frames { get; set; } = 10;
    [Key(5)]
    public string prompt { get; set; } = @"A cinematic video with smooth motion, natural lighting, and high detail.";
    [Key(6)]
    public bool remove_watermark { get; set; } = true;

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}
