using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Divide
{
    [Key(0)]
    public object a { get; set; } = 0.0;
    [Key(1)]
    public object b { get; set; } = 1.0;

    public object Process()
    {
        return default(object);
    }
}
