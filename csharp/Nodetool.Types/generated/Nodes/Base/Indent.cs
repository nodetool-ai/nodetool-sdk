using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Indent
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public string prefix { get; set; } = "    ";

    public string Process()
    {
        return default(string);
    }
}
