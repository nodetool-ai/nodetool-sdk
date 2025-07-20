using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class JoinPaths
{
    [Key(0)]
    public object paths { get; set; } = new List<object>();

    public Nodetool.Types.FilePath Process()
    {
        return default(Nodetool.Types.FilePath);
    }
}
