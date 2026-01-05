using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SliceImageGrid
{
    [Key(0)]
    public int columns { get; set; } = 0;
    [Key(1)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(2)]
    public int rows { get; set; } = 0;

    public object Process()
    {
        return default(object);
    }
}
