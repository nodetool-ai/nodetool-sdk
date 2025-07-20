using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Code;

[MessagePackObject]
public class EvaluateExpression
{
    [Key(0)]
    public string expression { get; set; } = "";
    [Key(1)]
    public object variables { get; set; } = new Dictionary<string, object>();

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
