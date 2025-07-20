using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class CreateSilence
{
    [Key(0)]
    public double duration { get; set; } = 1.0;

    public Nodetool.Types.AudioRef Process()
    {
        return default(Nodetool.Types.AudioRef);
    }
}
