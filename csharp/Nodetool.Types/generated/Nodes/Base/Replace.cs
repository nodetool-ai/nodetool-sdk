using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Text;

[MessagePackObject]
public class Replace
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public string old { get; set; } = "";
    [Key(2)]
    public string new { get; set; } = "";

    public string Process()
    {
        // Implementation would be generated based on node logic
        return default(string);
    }
}
