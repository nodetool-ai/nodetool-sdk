using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Filter
{
    [Key(0)]
    public object dictionary { get; set; } = new();
    [Key(1)]
    public object keys { get; set; } = new();

    public object Process()
    {
        return default(object);
    }
}
