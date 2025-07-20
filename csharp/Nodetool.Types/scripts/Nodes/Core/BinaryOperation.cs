using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class BinaryOperation
{
    [Key(0)]
    public object a { get; set; } = 0.0;
    [Key(1)]
    public object b { get; set; } = 0.0;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
