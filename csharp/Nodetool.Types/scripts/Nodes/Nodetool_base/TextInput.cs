using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class TextInput
{
    [Key(0)]
    public Nodetool.Types.TextRef value { get; set; } = new Nodetool.Types.TextRef();
    [Key(1)]
    public string name { get; set; } = "";
    [Key(2)]
    public string description { get; set; } = "";

    public Nodetool.Types.TextRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.TextRef);
    }
}
