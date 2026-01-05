using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class DataframeOutput
{
    [Key(0)]
    public string description { get; set; } = @"";
    [Key(1)]
    public string name { get; set; } = @"";
    [Key(2)]
    public Nodetool.Types.Core.DataframeRef value { get; set; } = new Nodetool.Types.Core.DataframeRef();

    public object Process()
    {
        return default(object);
    }
}
