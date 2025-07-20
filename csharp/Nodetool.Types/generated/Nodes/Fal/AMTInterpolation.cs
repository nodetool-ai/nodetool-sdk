using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class AMTInterpolation
{
    [Key(0)]
    public object frames { get; set; } = new List<object>();
    [Key(1)]
    public int output_fps { get; set; } = 24;
    [Key(2)]
    public int recursive_interpolation_passes { get; set; } = 4;

    public Nodetool.Types.VideoRef Process()
    {
        return default(Nodetool.Types.VideoRef);
    }
}
