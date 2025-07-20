using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GoogleFinance
{
    [Key(0)]
    public string query { get; set; } = "";
    [Key(1)]
    public string window { get; set; } = "";

    public object Process()
    {
        return default(object);
    }
}
