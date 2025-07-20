using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class Pivot
{
    [Key(0)]
    public Nodetool.Types.DataframeRef dataframe { get; set; } = new Nodetool.Types.DataframeRef();
    [Key(1)]
    public string index { get; set; } = "";
    [Key(2)]
    public string columns { get; set; } = "";
    [Key(3)]
    public string values { get; set; } = "";
    [Key(4)]
    public string aggfunc { get; set; } = "sum";

    public Nodetool.Types.DataframeRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.DataframeRef);
    }
}
