using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class PostRequest
{
    [Key(0)]
    public string url { get; set; } = "";
    [Key(1)]
    public string data { get; set; } = "";

    public string Process()
    {
        return default(string);
    }
}
