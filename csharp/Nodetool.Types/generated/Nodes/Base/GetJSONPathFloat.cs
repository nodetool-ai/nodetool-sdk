using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GetJSONPathFloat
{
    [Key(0)]
    public object data { get; set; } = null;
    [Key(1)]
    public double default_ { get; set; } = 0.0;
    [Key(2)]
    public string path { get; set; } = @"";

    public double Process()
    {
        return default(double);
    }
}
