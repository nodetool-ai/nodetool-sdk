using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class Bool
{
    [Key(0)]
    public bool value { get; set; } = false;

    public bool Process()
    {
        // Implementation would be generated based on node logic
        return default(bool);
    }
}
