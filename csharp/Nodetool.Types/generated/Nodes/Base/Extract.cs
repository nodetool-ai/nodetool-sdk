using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Extract
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public int start { get; set; } = 0;
    [Key(2)]
    public int end { get; set; } = 0;

    public string Process()
    {
        return default(string);
    }
}
