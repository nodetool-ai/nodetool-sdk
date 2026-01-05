using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class Flux2ProImageToImage
{
    [Key(0)]
    public object aspect_ratio { get; set; } = @"1:1";
    [Key(1)]
    public double guidance_scale { get; set; } = 7.5;
    [Key(2)]
    public Nodetool.Types.Core.ImageRef image { get; set; } = new Nodetool.Types.Core.ImageRef();
    [Key(3)]
    public string prompt { get; set; } = @"";
    [Key(4)]
    public object resolution { get; set; } = @"1K";
    [Key(5)]
    public int steps { get; set; } = 25;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
