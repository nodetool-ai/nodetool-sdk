using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class JSONPutRequest
{
    [Key(0)]
    public string url { get; set; } = "";
    [Key(1)]
    public object data { get; set; } = new Dictionary<string, object>();

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
