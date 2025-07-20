using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ExtractVideos
{
    [Key(0)]
    public string html { get; set; } = "";
    [Key(1)]
    public string base_url { get; set; } = "";

    public object Process()
    {
        return default(object);
    }
}
