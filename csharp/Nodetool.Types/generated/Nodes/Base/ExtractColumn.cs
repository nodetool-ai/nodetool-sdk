using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ExtractColumn
{
    [Key(0)]
    public string column_name { get; set; } = @"";
    [Key(1)]
    public Nodetool.Types.Core.DataframeRef dataframe { get; set; } = new Nodetool.Types.Core.DataframeRef();

    public object Process()
    {
        return default(object);
    }
}
