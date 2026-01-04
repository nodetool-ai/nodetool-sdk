using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Join
{
    [Key(0)]
    public string separator { get; set; } = @"";
    [Key(1)]
    public object strings { get; set; } = new();

    public string Process()
    {
        return default(string);
    }
}
