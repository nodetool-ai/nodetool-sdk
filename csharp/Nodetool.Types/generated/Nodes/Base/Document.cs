using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Document
{
    [Key(0)]
    public Nodetool.Types.DocumentRef value { get; set; } = new Nodetool.Types.DocumentRef();

    public Nodetool.Types.DocumentRef Process()
    {
        return default(Nodetool.Types.DocumentRef);
    }
}
