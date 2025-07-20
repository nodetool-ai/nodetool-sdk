using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class OverlayAudio
{
    [Key(0)]
    public Nodetool.Types.AudioRef a { get; set; } = new Nodetool.Types.AudioRef();
    [Key(1)]
    public Nodetool.Types.AudioRef b { get; set; } = new Nodetool.Types.AudioRef();

    public Nodetool.Types.AudioRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.AudioRef);
    }
}
