using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Repeat
{
    [Key(0)]
    public Nodetool.Types.Core.AudioRef audio { get; set; } = new Nodetool.Types.Core.AudioRef();
    [Key(1)]
    public int loops { get; set; } = 2;

    public Nodetool.Types.Core.AudioRef Process()
    {
        return default(Nodetool.Types.Core.AudioRef);
    }
}
