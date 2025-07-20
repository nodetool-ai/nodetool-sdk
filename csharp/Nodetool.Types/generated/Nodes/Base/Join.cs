using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Join
{
    [Key(0)]
    public object strings { get; set; } = new List<object>();
    [Key(1)]
    public string separator { get; set; } = "";

    public string Process()
    {
        return default(string);
    }
}
