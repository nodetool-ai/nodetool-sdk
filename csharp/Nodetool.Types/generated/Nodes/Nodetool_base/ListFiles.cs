using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Types.Nodes.Nodetool_base;

[MessagePackObject]
public class ListFiles
{
    [Key(0)]
    public Nodetool.Types.FilePath directory { get; set; } = new Nodetool.Types.FilePath();
    [Key(1)]
    public string pattern { get; set; } = "*";
    [Key(2)]
    public bool recursive { get; set; } = false;

    public object Process()
    {
        // Implementation would be generated based on node logic
        return default(object);
    }
}
