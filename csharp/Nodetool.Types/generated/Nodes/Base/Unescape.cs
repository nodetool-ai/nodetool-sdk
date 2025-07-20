using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Unescape
{
    [Key(0)]
    public string text { get; set; } = "";

    public string Process()
    {
        return default(string);
    }
}
