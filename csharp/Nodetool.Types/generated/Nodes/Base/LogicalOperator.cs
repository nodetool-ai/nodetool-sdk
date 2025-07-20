using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class LogicalOperator
{
    [Key(0)]
    public bool a { get; set; } = false;
    [Key(1)]
    public bool b { get; set; } = false;
    [Key(2)]
    public object operation { get; set; } = "BooleanOperation.AND";

    public bool Process()
    {
        return default(bool);
    }
}
