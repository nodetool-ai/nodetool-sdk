using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class HeadRequest
{
    [Key(0)]
    public string url { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
