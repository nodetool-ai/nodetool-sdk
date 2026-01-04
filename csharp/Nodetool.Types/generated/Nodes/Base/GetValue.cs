using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GetValue
{
    [Key(0)]
    public object default_ { get; set; } = null;
    [Key(1)]
    public object dictionary { get; set; } = new();
    [Key(2)]
    public string key { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
