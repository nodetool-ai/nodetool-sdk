using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GaussianBlur
{
    [Key(0)]
    public double std_deviation { get; set; } = 3.0;

    public Nodetool.Types.Core.SVGElement Process()
    {
        return default(Nodetool.Types.Core.SVGElement);
    }
}
