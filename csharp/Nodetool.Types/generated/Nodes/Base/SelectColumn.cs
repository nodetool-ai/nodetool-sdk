using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SelectColumn
{
    [Key(0)]
    public string columns { get; set; } = @"";
    [Key(1)]
    public Nodetool.Types.Core.DataframeRef dataframe { get; set; } = new Nodetool.Types.Core.DataframeRef();

    public Nodetool.Types.Core.DataframeRef Process()
    {
        return default(Nodetool.Types.Core.DataframeRef);
    }
}
