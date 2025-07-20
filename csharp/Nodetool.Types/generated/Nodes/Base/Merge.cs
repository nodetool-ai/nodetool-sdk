using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Merge
{
    [Key(0)]
    public Nodetool.Types.DataframeRef dataframe_a { get; set; } = new Nodetool.Types.DataframeRef();
    [Key(1)]
    public Nodetool.Types.DataframeRef dataframe_b { get; set; } = new Nodetool.Types.DataframeRef();

    public Nodetool.Types.DataframeRef Process()
    {
        return default(Nodetool.Types.DataframeRef);
    }
}
