using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class RandomInt
{
    [Key(0)]
    public int minimum { get; set; } = 0;
    [Key(1)]
    public int maximum { get; set; } = 100;

    public int Process()
    {
        // Implementation would be generated based on node logic
        return default(int);
    }
}
