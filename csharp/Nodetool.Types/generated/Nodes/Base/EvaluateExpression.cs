using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class EvaluateExpression
{
    [Key(0)]
    public string expression { get; set; } = "";
    [Key(1)]
    public object variables { get; set; } = new Dictionary<string, object>();

    public object Process()
    {
        return default(object);
    }
}
