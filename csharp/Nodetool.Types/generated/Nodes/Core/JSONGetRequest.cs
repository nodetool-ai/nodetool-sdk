using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class JSONGetRequest
{
    [Key(0)]
    public string url { get; set; } = "";

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
