using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class GetJSONPathBool
{
    [Key(0)]
    public object data { get; set; } = null;
    [Key(1)]
    public string path { get; set; } = "";
    [Key(2)]
    public bool default { get; set; } = false;

    public bool Process()
    {
        // Implementation would be generated based on node logic
        return default(bool);
    }
}
