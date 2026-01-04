using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Compare
{
    [Key(0)]
    public object a { get; set; } = null;
    [Key(1)]
    public object b { get; set; } = null;
    [Key(2)]
    public object comparison { get; set; } = @"==";

    public bool Process()
    {
        return default(bool);
    }
}
