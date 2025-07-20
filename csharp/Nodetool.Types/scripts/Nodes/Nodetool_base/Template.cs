using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class Template
{
    [Key(0)]
    public string string { get; set; } = "";
    [Key(1)]
    public object values { get; set; } = new Dictionary<string, object>();

    public string Process()
    {
        // Implementation would be generated based on node logic
        return default(string);
    }
}
