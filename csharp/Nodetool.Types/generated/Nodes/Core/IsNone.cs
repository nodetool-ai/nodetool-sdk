using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class IsNone
{
    [Key(0)]
    public object value { get; set; } = null;

    public bool Process()
    {
        // Implementation would be generated based on node logic
        return default(bool);
    }
}
