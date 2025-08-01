using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class RelativePath
{
    [Key(0)]
    public string target_path { get; set; } = "";
    [Key(1)]
    public string start_path { get; set; } = ".";

    public Nodetool.Types.FilePath Process()
    {
        return default(Nodetool.Types.FilePath);
    }
}
