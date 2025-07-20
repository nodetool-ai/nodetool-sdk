using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class FormatText
{
    [Key(0)]
    public string template { get; set; } = "";

    public string Process()
    {
        return default(string);
    }
}
