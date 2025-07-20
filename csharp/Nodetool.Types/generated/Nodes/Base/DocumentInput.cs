using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Input;

[MessagePackObject]
public class DocumentInput
{
    [Key(0)]
    public Nodetool.Types.DocumentRef value { get; set; } = new Nodetool.Types.DocumentRef();
    [Key(1)]
    public string name { get; set; } = "";
    [Key(2)]
    public string description { get; set; } = "";

    public Nodetool.Types.DocumentRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.DocumentRef);
    }
}
