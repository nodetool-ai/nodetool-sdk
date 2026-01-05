using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ChromaKey
{
    [Key(0)]
    public double blend { get; set; } = 0.1;
    [Key(1)]
    public Nodetool.Types.Core.ColorRef key_color { get; set; } = new Nodetool.Types.Core.ColorRef();
    [Key(2)]
    public double similarity { get; set; } = 0.3;
    [Key(3)]
    public Nodetool.Types.Core.VideoRef video { get; set; } = new Nodetool.Types.Core.VideoRef();

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}
