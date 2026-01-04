using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Background
{
    [Key(0)]
    public Nodetool.Types.Core.ColorRef color { get; set; } = new Nodetool.Types.Core.ColorRef();
    [Key(1)]
    public int height { get; set; } = 512;
    [Key(2)]
    public int width { get; set; } = 512;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
