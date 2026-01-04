using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GoogleImages
{
    [Key(0)]
    public string image_url { get; set; } = @"";
    [Key(1)]
    public string keyword { get; set; } = @"";
    [Key(2)]
    public int num_results { get; set; } = 20;

    public object Process()
    {
        return default(object);
    }
}
