using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

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
        return default(object);
    }
}
