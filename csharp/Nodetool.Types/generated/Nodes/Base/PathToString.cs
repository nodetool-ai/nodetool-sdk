using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class PathToString
{
    [Key(0)]
    public Nodetool.Types.FilePath file_path { get; set; } = new Nodetool.Types.FilePath();

    public string Process()
    {
        return default(string);
    }
}
