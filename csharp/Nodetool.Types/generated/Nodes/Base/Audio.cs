using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Audio
{
    [Key(0)]
    public Nodetool.Types.AudioRef value { get; set; } = new Nodetool.Types.AudioRef();

    public Nodetool.Types.AudioRef Process()
    {
        return default(Nodetool.Types.AudioRef);
    }
}
