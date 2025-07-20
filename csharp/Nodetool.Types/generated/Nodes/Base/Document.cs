using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Constant;

[MessagePackObject]
public class Document
{
    [Key(0)]
    public Nodetool.Types.DocumentRef value { get; set; } = new Nodetool.Types.DocumentRef();

    public Nodetool.Types.DocumentRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.DocumentRef);
    }
}
