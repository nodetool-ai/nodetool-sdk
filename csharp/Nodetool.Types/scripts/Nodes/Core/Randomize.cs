using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class Randomize
{
    [Key(0)]
    public object values { get; set; } = new List<object>();

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
