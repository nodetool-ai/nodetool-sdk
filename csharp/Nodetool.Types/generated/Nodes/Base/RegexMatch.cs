using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class RegexMatch
{
    [Key(0)]
    public int? group { get; set; } = null;
    [Key(1)]
    public string pattern { get; set; } = @"";
    [Key(2)]
    public string text { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
