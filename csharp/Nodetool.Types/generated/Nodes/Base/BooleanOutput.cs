using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class BooleanOutput
{
    [Key(0)]
    public string description { get; set; } = @"";
    [Key(1)]
    public string name { get; set; } = @"";
    [Key(2)]
    public bool value { get; set; } = false;

    public object Process()
    {
        return default(object);
    }
}
