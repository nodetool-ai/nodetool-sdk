using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class HashString
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public string algorithm { get; set; } = "md5";

    public string Process()
    {
        return default(string);
    }
}
