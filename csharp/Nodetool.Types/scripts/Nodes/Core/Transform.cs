using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Core;

[MessagePackObject]
public class Transform
{
    [Key(0)]
    public object values { get; set; } = new List<object>();
    [Key(1)]
    public object transform_type { get; set; } = "TransformType.TO_STRING";

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
