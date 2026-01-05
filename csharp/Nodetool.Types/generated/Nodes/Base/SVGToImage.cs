using MessagePack;
using System.Collections.Generic;
using Nodetool.Types;

namespace Nodetool.Nodes.Base;

[MessagePackObject]
public class SVGToImage
{
    [Key(0)]
    public object content { get; set; } = new();
    [Key(1)]
    public int height { get; set; } = 600;
    [Key(2)]
    public int scale { get; set; } = 1;
    [Key(3)]
    public string viewBox { get; set; } = @"0 0 800 600";
    [Key(4)]
    public int width { get; set; } = 800;

    public Nodetool.Types.Core.ImageRef Process()
    {
        return default(Nodetool.Types.Core.ImageRef);
    }
}
