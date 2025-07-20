using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class WebFetch
{
    [Key(0)]
    public string url { get; set; } = "";
    [Key(1)]
    public string selector { get; set; } = "body";

    public string Process()
    {
        // Implementation would be generated based on node logic
        return default(string);
    }
}
