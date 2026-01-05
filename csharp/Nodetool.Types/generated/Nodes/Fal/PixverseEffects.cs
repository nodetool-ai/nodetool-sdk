using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Fal;

[MessagePackObject]
public class PixverseEffects
{
    [Key(0)]
    public string prompt { get; set; } = @"";
    [Key(1)]
    public int seed { get; set; } = -1;
    [Key(2)]
    public Nodetool.Types.Core.VideoRef video { get; set; } = new Nodetool.Types.Core.VideoRef();

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}
