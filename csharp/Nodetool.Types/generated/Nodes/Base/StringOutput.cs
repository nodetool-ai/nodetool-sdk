using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class StringOutput
{
    [Key(0)]
    public string description { get; set; } = @"";
    [Key(1)]
    public string name { get; set; } = @"";
    [Key(2)]
    public string value { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
