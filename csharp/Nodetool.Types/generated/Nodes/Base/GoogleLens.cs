using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class GoogleLens
{
    [Key(0)]
    public string image_url { get; set; } = @"";
    [Key(1)]
    public int num_results { get; set; } = 10;

    public void Process()
    {
    }
}
