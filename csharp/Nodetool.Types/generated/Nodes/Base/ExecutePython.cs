using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ExecutePython
{
    [Key(0)]
    public string code { get; set; } = "";
    [Key(1)]
    public object inputs { get; set; } = new Dictionary<string, object>();

    public object Process()
    {
        return default(object);
    }
}
