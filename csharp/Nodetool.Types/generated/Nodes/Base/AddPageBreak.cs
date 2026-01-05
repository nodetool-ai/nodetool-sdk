using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class AddPageBreak
{
    [Key(0)]
    public Nodetool.Types.Core.DocumentRef document { get; set; } = new Nodetool.Types.Core.DocumentRef();

    public Nodetool.Types.Core.DocumentRef Process()
    {
        return default(Nodetool.Types.Core.DocumentRef);
    }
}
