using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GaussianNoise
{
    [Key(0)]
    public int height { get; set; } = 512;
    [Key(1)]
    public double mean { get; set; } = 0.0;
    [Key(2)]
    public double stddev { get; set; } = 1.0;
    [Key(3)]
    public int width { get; set; } = 512;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
