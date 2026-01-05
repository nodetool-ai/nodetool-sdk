using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class RegexReplace
{
    [Key(0)]
    public int count { get; set; } = 0;
    [Key(1)]
    public string pattern { get; set; } = @"";
    [Key(2)]
    public string replacement { get; set; } = @"";
    [Key(3)]
    public string text { get; set; } = @"";

    public string Process()
    {
        return default(string);
    }
}
