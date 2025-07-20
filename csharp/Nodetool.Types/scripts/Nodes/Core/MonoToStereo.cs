using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class MonoToStereo
{
    [Key(0)]
    public Nodetool.Types.AudioRef audio { get; set; } = new Nodetool.Types.AudioRef();

    public Nodetool.Types.AudioRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.AudioRef);
    }
}
