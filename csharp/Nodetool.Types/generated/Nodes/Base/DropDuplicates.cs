using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DropDuplicates
{
    [Key(0)]
    public Nodetool.Types.DataframeRef df { get; set; } = new Nodetool.Types.DataframeRef();

    public Nodetool.Types.DataframeRef Process()
    {
        return default(Nodetool.Types.DataframeRef);
    }
}
