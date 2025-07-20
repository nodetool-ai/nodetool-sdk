using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class GetJSONPathDict
{
    [Key(0)]
    public object data { get; set; } = null;
    [Key(1)]
    public string path { get; set; } = "";
    [Key(2)]
    public object default { get; set; } = new Dictionary<string, object>();

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
