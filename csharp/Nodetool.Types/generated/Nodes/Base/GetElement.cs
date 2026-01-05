using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GetElement
{
    [Key(0)]
    public int index { get; set; } = 0;
    [Key(1)]
    public object values { get; set; } = new();

    public object Process()
    {
        return default(object);
    }
}
