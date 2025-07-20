using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Data;

[MessagePackObject]
public class DropNA
{
    [Key(0)]
    public Nodetool.Types.DataframeRef df { get; set; } = new Nodetool.Types.DataframeRef();

    public Nodetool.Types.DataframeRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.DataframeRef);
    }
}
