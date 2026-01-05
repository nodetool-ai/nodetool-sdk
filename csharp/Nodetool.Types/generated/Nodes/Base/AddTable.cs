using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class AddTable
{
    [Key(0)]
    public Nodetool.Types.Core.DataframeRef data { get; set; } = new Nodetool.Types.Core.DataframeRef();
    [Key(1)]
    public Nodetool.Types.Core.DocumentRef document { get; set; } = new Nodetool.Types.Core.DocumentRef();

    public Nodetool.Types.Core.DocumentRef Process()
    {
        return default(Nodetool.Types.Core.DocumentRef);
    }
}
