using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class CombineImageGrid
{
    [Key(0)]
    public int columns { get; set; } = 0;
    [Key(1)]
    public object tiles { get; set; } = new();

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
