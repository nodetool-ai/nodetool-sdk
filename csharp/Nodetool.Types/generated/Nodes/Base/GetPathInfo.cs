using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GetPathInfo
{
    [Key(0)]
    public string path { get; set; } = "";

    public object Process()
    {
        return default(object);
    }
}
