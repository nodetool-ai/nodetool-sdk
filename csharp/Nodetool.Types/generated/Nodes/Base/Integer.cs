using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Integer
{
    [Key(0)]
    public int value { get; set; } = 0;

    public int Process()
    {
        return default(int);
    }
}
