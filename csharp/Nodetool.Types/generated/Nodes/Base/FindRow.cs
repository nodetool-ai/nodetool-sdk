using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FindRow
{
    [Key(0)]
    public Nodetool.Types.DataframeRef df { get; set; } = new Nodetool.Types.DataframeRef();
    [Key(1)]
    public string condition { get; set; } = "";

    public Nodetool.Types.DataframeRef Process()
    {
        return default(Nodetool.Types.DataframeRef);
    }
}
