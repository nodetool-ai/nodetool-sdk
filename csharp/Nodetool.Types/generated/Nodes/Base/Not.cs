using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Not
{
    [Key(0)]
    public bool value { get; set; } = false;

    public bool Process()
    {
        return default(bool);
    }
}
