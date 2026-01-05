using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class AddColumn
{
    [Key(0)]
    public string column_name { get; set; } = @"";
    [Key(1)]
    public Nodetool.Types.Core.DataframeRef dataframe { get; set; } = new Nodetool.Types.Core.DataframeRef();
    [Key(2)]
    public object values { get; set; } = new();

    public Nodetool.Types.Core.DataframeRef Process()
    {
        return default(Nodetool.Types.Core.DataframeRef);
    }
}
