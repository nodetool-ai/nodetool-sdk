using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FindRow
{
    [Key(0)]
    public string condition { get; set; } = @"";
    [Key(1)]
    public Nodetool.Types.Core.DataframeRef df { get; set; } = new Nodetool.Types.Core.DataframeRef();

    public Nodetool.Types.Core.DataframeRef Process()
    {
        return default(Nodetool.Types.Core.DataframeRef);
    }
}
