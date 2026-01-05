using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class RPC
{
    [Key(0)]
    public string function { get; set; } = @"";
    [Key(1)]
    public object params_ { get; set; } = null;
    [Key(2)]
    public bool to_dataframe { get; set; } = false;

    public object Process()
    {
        return default(object);
    }
}
