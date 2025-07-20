using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ExtractRegex
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public string regex { get; set; } = "";
    [Key(2)]
    public bool dotall { get; set; } = false;
    [Key(3)]
    public bool ignorecase { get; set; } = false;
    [Key(4)]
    public bool multiline { get; set; } = false;

    public object Process()
    {
        return default(object);
    }
}
