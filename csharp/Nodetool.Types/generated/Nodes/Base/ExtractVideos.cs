using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class ExtractVideos
{
    [Key(0)]
    public string base_url { get; set; } = @"";
    [Key(1)]
    public string html { get; set; } = @"";

    public Nodetool.Types.Core.VideoRef Process()
    {
        return default(Nodetool.Types.Core.VideoRef);
    }
}
