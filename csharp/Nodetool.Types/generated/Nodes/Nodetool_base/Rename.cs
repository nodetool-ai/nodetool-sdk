using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class Rename
{
    [Key(0)]
    public Nodetool.Types.DataframeRef dataframe { get; set; } = new Nodetool.Types.DataframeRef();
    [Key(1)]
    public string rename_map { get; set; } = "";

    public Nodetool.Types.DataframeRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.DataframeRef);
    }
}
