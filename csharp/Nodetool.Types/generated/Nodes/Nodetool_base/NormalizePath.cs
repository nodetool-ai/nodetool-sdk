using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class NormalizePath
{
    [Key(0)]
    public string path { get; set; } = "";

    public Nodetool.Types.FilePath Process()
    {
        // Implementation would be generated based on node logic
        return default(Nodetool.Types.FilePath);
    }
}
