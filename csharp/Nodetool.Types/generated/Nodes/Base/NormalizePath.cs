using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class NormalizePath
{
    [Key(0)]
    public string path { get; set; } = "";

    public Nodetool.Types.FilePath Process()
    {
        return default(Nodetool.Types.FilePath);
    }
}
