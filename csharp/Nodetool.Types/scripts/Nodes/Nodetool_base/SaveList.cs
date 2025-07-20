using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class SaveList
{
    [Key(0)]
    public object values { get; set; } = new List<object>();
    [Key(1)]
    public string name { get; set; } = "text.txt";

    public Nodetool.Types.TextRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.TextRef);
    }
}
