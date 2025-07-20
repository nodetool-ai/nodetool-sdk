using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ValidateJSON
{
    [Key(0)]
    public object data { get; set; } = null;
    [Key(1)]
    public object json_schema { get; set; } = new Dictionary<string, object>();

    public bool Process()
    {
        return default(bool);
    }
}
