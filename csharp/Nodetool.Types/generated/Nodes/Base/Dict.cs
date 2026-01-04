using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Dict
{
    [Key(0)]
    public object value { get; set; } = new();

    public object Process()
    {
        return default(object);
    }
}
