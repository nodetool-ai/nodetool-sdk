using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Sine
{
    [Key(0)]
    public object angle_rad { get; set; } = 0.0;

    public object Process()
    {
        return default(object);
    }
}
