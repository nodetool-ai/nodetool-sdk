using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DataFrame
{
    [Key(0)]
    public Nodetool.Types.Core.DataframeRef value { get; set; } = new Nodetool.Types.Core.DataframeRef();

    public Nodetool.Types.Core.DataframeRef Process()
    {
        return default(Nodetool.Types.Core.DataframeRef);
    }
}
