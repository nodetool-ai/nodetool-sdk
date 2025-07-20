using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class AddColumn
{
    [Key(0)]
    public Nodetool.Types.DataframeRef dataframe { get; set; } = new Nodetool.Types.DataframeRef();
    [Key(1)]
    public string column_name { get; set; } = "";
    [Key(2)]
    public object values { get; set; } = new List<object>();

    public Nodetool.Types.DataframeRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.DataframeRef);
    }
}
