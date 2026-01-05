using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Pivot
{
    [Key(0)]
    public string aggfunc { get; set; } = @"sum";
    [Key(1)]
    public string columns { get; set; } = @"";
    [Key(2)]
    public Nodetool.Types.Core.DataframeRef dataframe { get; set; } = new Nodetool.Types.Core.DataframeRef();
    [Key(3)]
    public string index { get; set; } = @"";
    [Key(4)]
    public string values { get; set; } = @"";

    public Nodetool.Types.Core.DataframeRef Process()
    {
        return default(Nodetool.Types.Core.DataframeRef);
    }
}
