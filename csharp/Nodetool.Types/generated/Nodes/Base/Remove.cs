using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Remove
{
    [Key(0)]
    public object dictionary { get; set; } = new Dictionary<string, object>();
    [Key(1)]
    public string key { get; set; } = "";

    public object Process()
    {
        return default(object);
    }
}
