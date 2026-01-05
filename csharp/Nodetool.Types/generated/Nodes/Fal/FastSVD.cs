using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class FastSVD
{
    [Key(0)]
    public double cond_aug { get; set; } = 0.02;
    [Key(1)]
    public int fps { get; set; } = 10;
    [Key(2)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(3)]
    public int motion_bucket_id { get; set; } = 127;
    [Key(4)]
    public int seed { get; set; } = -1;
    [Key(5)]
    public int steps { get; set; } = 4;

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}
