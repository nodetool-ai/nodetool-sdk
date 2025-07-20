using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class WebSearch
{
    [Key(0)]
    public string query { get; set; } = "";

    public string Process()
    {
        return default(string);
    }
}
