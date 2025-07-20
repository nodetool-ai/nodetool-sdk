using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GetSecret
{
    [Key(0)]
    public string name { get; set; } = "";
    [Key(1)]
    public object default { get; set; } = null;

    public object Process()
    {
        return default(object);
    }
}
