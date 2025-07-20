using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class JSONPatchRequest
{
    [Key(0)]
    public string url { get; set; } = "";
    [Key(1)]
    public object data { get; set; } = new Dictionary<string, object>();

    public object Process()
    {
        return default(object);
    }
}
