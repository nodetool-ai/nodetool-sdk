using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class ConvertToArray
{
    [Key(0)]
    public Nodetool.Types.AudioRef audio { get; set; } = new Nodetool.Types.AudioRef();

    public Nodetool.Types.NPArray Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.NPArray);
    }
}
