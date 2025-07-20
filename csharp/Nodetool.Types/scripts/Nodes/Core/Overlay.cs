using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class Overlay
{
    [Key(0)]
    public Nodetool.Types.VideoRef main_video { get; set; } = new Nodetool.Types.VideoRef();
    [Key(1)]
    public Nodetool.Types.VideoRef overlay_video { get; set; } = new Nodetool.Types.VideoRef();
    [Key(2)]
    public int x { get; set; } = 0;
    [Key(3)]
    public int y { get; set; } = 0;
    [Key(4)]
    public double scale { get; set; } = 1.0;
    [Key(5)]
    public double overlay_audio_volume { get; set; } = 0.5;

    public Nodetool.Types.VideoRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.VideoRef);
    }
}
