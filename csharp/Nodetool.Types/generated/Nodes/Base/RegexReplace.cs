using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class RegexReplace
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public string pattern { get; set; } = "";
    [Key(2)]
    public string replacement { get; set; } = "";
    [Key(3)]
    public int count { get; set; } = 0;

    public string Process()
    {
        return default(string);
    }
}
