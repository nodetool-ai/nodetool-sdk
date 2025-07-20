using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Search;

[MessagePackObject]
public class GoogleSearch
{
    [Key(0)]
    public string keyword { get; set; } = "";
    [Key(1)]
    public int num_results { get; set; } = 10;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
