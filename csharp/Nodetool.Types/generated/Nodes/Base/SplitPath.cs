using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class SplitPath
{
    [Key(0)]
    public string path { get; set; } = "";

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
