using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class MapField
{
    [Key(0)]
    public object default_ { get; set; } = null;
    [Key(1)]
    public string field { get; set; } = @"";
    [Key(2)]
    public object values { get; set; } = new();

    public object Process()
    {
        return default(object);
    }
}
