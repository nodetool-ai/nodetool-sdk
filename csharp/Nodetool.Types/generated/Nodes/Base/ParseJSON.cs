using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ParseJSON
{
    [Key(0)]
    public string text { get; set; } = "";

    public object Process()
    {
        return default(object);
    }
}
