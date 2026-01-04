using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Merge
{
    [Key(0)]
    public Nodetool.Types.Core.DataframeRef dataframe_a { get; set; } = new Nodetool.Types.Core.DataframeRef();
    [Key(1)]
    public Nodetool.Types.Core.DataframeRef dataframe_b { get; set; } = new Nodetool.Types.Core.DataframeRef();

    public Nodetool.Types.Core.DataframeRef Process()
    {
        return default(Nodetool.Types.Core.DataframeRef);
    }
}
