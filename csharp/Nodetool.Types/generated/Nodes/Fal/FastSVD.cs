using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class FastSVD
{
    [Key(0)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(1)]
    public int motion_bucket_id { get; set; } = 127;
    [Key(2)]
    public double cond_aug { get; set; } = 0.02;
    [Key(3)]
    public int steps { get; set; } = 4;
    [Key(4)]
    public int fps { get; set; } = 10;
    [Key(5)]
    public int seed { get; set; } = -1;

    public Nodetool.Types.VideoRef Process()
    {
        return default(Nodetool.Types.VideoRef);
    }
}
