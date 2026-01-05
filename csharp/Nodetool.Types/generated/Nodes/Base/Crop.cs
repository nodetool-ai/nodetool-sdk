using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Crop
{
    [Key(0)]
    public int bottom { get; set; } = 512;
    [Key(1)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(2)]
    public int left { get; set; } = 0;
    [Key(3)]
    public int right { get; set; } = 512;
    [Key(4)]
    public int top { get; set; } = 0;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
