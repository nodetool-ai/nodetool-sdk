using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class Indent
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public string prefix { get; set; } = "    ";

    public string Process()
    {
        // Implementation would be generated based on node logic
        return default(string);
    }
}
