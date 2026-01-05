using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FindAllRegex
{
    [Key(0)]
    public bool dotall { get; set; } = false;
    [Key(1)]
    public bool ignorecase { get; set; } = false;
    [Key(2)]
    public bool multiline { get; set; } = false;
    [Key(3)]
    public string regex { get; set; } = @"";
    [Key(4)]
    public string text { get; set; } = @"";

    public object Process()
    {
        return default(object);
    }
}
