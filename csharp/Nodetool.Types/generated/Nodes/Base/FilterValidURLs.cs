using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FilterValidURLs
{
    [Key(0)]
    public int max_concurrent_requests { get; set; } = 10;
    [Key(1)]
    public string url { get; set; } = @"";
    [Key(2)]
    public object urls { get; set; } = new();

    public object Process()
    {
        return default(object);
    }
}
