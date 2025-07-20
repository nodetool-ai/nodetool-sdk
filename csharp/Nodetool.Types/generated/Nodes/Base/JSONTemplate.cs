using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class JSONTemplate
{
    [Key(0)]
    public string template { get; set; } = "";
    [Key(1)]
    public object values { get; set; } = new Dictionary<string, object>();

    public object Process()
    {
        return default(object);
    }
}
