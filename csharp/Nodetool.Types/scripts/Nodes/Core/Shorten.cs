using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class Shorten
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public int width { get; set; } = 70;
    [Key(2)]
    public string placeholder { get; set; } = "...";

    public string Process()
    {
        // Implementation would be generated based on node logic
        return default(string);
    }
}
