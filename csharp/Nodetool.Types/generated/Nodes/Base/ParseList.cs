using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ParseList
{
    [Key(0)]
    public string json_string { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
