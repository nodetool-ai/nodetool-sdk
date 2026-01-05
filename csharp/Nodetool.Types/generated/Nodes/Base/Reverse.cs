using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Reverse
{
    [Key(0)]
    public Nodetool.Types.Core.VideoRef video { get; set; } = new Nodetool.Types.Core.VideoRef();

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}
