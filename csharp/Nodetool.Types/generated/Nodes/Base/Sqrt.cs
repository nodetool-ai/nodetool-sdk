using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Sqrt
{
    [Key(0)]
    public object x { get; set; } = 1.0;

    public object Process()
    {
        return default(object);
    }
}
