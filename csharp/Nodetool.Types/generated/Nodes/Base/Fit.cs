using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Fit
{
    [Key(0)]
    public Nodetool.Types.ImageRef image { get; set; } = new Nodetool.Types.ImageRef();
    [Key(1)]
    public int width { get; set; } = 512;
    [Key(2)]
    public int height { get; set; } = 512;

    public Nodetool.Types.ImageRef Process()
    {
        return default(Nodetool.Types.ImageRef);
    }
}
