using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Basename
{
    [Key(0)]
    public string path { get; set; } = "";
    [Key(1)]
    public bool remove_extension { get; set; } = false;

    public string Process()
    {
        return default(string);
    }
}
