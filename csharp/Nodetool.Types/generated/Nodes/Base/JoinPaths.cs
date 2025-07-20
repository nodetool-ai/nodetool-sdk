using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Lib;

[MessagePackObject]
public class JoinPaths
{
    [Key(0)]
    public object paths { get; set; } = new List<object>();

    public Nodetool.Types.FilePath Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.FilePath);
    }
}
