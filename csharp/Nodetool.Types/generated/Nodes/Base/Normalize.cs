using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Normalize
{
    [Key(0)]
    public Nodetool.Types.Core.AudioRef audio { get; set; } = new Nodetool.Types.Core.AudioRef();

    public Nodetool.Types.Core.AudioRef Process()
    {
        return default(Nodetool.Types.Core.AudioRef);
    }
}
