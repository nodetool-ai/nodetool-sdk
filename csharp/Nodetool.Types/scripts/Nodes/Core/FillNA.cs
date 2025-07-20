using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class FillNA
{
    [Key(0)]
    public Nodetool.Types.DataframeRef dataframe { get; set; } = new Nodetool.Types.DataframeRef();
    [Key(1)]
    public object value { get; set; } = 0;
    [Key(2)]
    public string method { get; set; } = "value";
    [Key(3)]
    public string columns { get; set; } = "";

    public Nodetool.Types.DataframeRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.DataframeRef);
    }
}
