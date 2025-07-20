using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GetRequest
{
    [Key(0)]
    public string url { get; set; } = "";

    public string Process()
    {
        return default(string);
    }
}
