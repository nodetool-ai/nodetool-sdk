using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class RegexValidate
{
    [Key(0)]
    public string text { get; set; } = "";
    [Key(1)]
    public string pattern { get; set; } = "";

    public bool Process()
    {
        return default(bool);
    }
}
