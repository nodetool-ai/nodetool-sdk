using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class PutRequest
{
    [Key(0)]
    public string url { get; set; } = "";
    [Key(1)]
    public string data { get; set; } = "";

    public string Process()
    {
        // Implementation would be generated based on node logic
        return default(string);
    }
}
