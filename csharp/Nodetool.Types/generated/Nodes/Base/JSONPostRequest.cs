using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class JSONPostRequest
{
    [Key(0)]
    public object data { get; set; } = new();
    [Key(1)]
    public string url { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
