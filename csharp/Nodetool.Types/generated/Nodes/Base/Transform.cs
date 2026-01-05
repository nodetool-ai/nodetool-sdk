using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Transform
{
    [Key(0)]
    public object transform_type { get; set; } = @"to_string";
    [Key(1)]
    public object values { get; set; } = new();

    public object Process()
    {
        return default(object);
    }
}
