using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GoogleMaps
{
    [Key(0)]
    public int num_results { get; set; } = 10;
    [Key(1)]
    public string query { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
