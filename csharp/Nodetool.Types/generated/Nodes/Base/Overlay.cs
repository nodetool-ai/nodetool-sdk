using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Overlay
{
    [Key(0)]
    public Nodetool.Types.Core.VideoRef main_video { get; set; } = new Nodetool.Types.Core.VideoRef();
    [Key(1)]
    public double overlay_audio_volume { get; set; } = 0.5;
    [Key(2)]
    public Nodetool.Types.Core.VideoRef overlay_video { get; set; } = new Nodetool.Types.Core.VideoRef();
    [Key(3)]
    public double scale { get; set; } = 1.0;
    [Key(4)]
    public int x { get; set; } = 0;
    [Key(5)]
    public int y { get; set; } = 0;

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}
