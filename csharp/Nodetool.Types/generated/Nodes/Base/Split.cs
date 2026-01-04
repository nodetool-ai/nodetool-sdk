using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Split
{
    [Key(0)]
    public string delimiter { get; set; } = @",";
    [Key(1)]
    public string text { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
