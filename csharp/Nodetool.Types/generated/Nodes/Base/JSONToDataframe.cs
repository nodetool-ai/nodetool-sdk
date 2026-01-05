using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class JSONToDataframe
{
    [Key(0)]
    public string text { get; set; } = @"";

    public Nodetool.Types.Core.DataframeRef Process()
    {
        return default(Nodetool.Types.Core.DataframeRef);
    }
}
