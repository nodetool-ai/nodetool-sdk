using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class RandomInt
{
    [Key(0)]
    public int minimum { get; set; } = 0;
    [Key(1)]
    public int maximum { get; set; } = 100;

    public int Process()
    {
        return default(int);
    }
}
