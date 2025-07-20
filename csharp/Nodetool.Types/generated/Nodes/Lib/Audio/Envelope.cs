using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class Envelope
{
    [Key(0)]
    public Nodetool.Types.AudioRef audio { get; set; } = new Nodetool.Types.AudioRef();
    [Key(1)]
    public double attack { get; set; } = 0.1;
    [Key(2)]
    public double decay { get; set; } = 0.3;
    [Key(3)]
    public double release { get; set; } = 0.5;
    [Key(4)]
    public double peak_amplitude { get; set; } = 1.0;

    public Nodetool.Types.AudioRef Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.AudioRef);
    }
}
