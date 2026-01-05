using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ListToArray
{
    [Key(0)]
    public object values { get; set; } = new();

    public Nodetool.Types.Core.NPArray Process()
    {
        return default(Nodetool.Types.Core.NPArray);
    }
}
