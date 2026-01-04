using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Length
{
    [Key(0)]
    public object values { get; set; } = new();

    public int Process()
    {
        return default(int);
    }
}
