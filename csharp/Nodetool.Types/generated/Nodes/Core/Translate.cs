using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class Translate
{
    [Key(0)]
    public Nodetool.Types.AudioRef audio { get; set; } = new Nodetool.Types.AudioRef();
    [Key(1)]
    public double temperature { get; set; } = 0.0;

    public string Process()
    {
        // Implementation would be generated based on node logic
        return default(string);
    }
}
