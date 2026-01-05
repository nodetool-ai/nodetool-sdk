using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Reroute
{
    [Key(0)]
    public object input_value { get; set; } = null;

    public object Process()
    {
        return default(object);
    }
}
