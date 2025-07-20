using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class ShowNotification
{
    [Key(0)]
    public string title { get; set; } = "";
    [Key(1)]
    public string message { get; set; } = "";
    [Key(2)]
    public int timeout { get; set; } = 10;

    public void Process()
    {
        // Implementation would be generated based on node logic
    }
}
