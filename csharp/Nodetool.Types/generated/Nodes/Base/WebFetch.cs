using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class WebFetch
{
    [Key(0)]
    public string url { get; set; } = "";
    [Key(1)]
    public string selector { get; set; } = "body";

    public string Process()
    {
        return default(string);
    }
}
