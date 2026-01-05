using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class UnsharpMask
{
    [Key(0)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(1)]
    public int percent { get; set; } = 150;
    [Key(2)]
    public int radius { get; set; } = 2;
    [Key(3)]
    public int threshold { get; set; } = 3;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
